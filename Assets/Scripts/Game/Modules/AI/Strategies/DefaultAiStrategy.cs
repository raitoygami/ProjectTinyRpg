using System;
using System.Collections.Generic;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
///     默认策略：视野内先按 ThreatTime 警觉倒计时，接战追击；追击厌倦时若目标已脱离出生点周围范围则返回出生格，否则 Idle。
///     <para>
///         行为树（成功=true，失败=false）：
///         Selector(
///         HandleReturningHome（脱离回巢 / 玩家重回范围则交回主流程）,
///         Selector(
///         无视野敌人 → ResetIdle,
///         Sequencer(BindTarget, ApplyPhase, EngagedLayer)
///         )
///         )
///     </para>
/// </summary>
[AiStrategy(AiPattern.Default)]
public sealed class DefaultAiStrategy : IAiStrategy
{
    private AIEntity _owner;
    private Blackboard _board;
    private readonly DefaultState _state = new();

    private sealed class DefaultState
    {
        public AiPhaseDefault PhaseDefault;
        public int SuspicionRemaining;
        public int ChaseTurns;
        public Entity LastTrackedTarget;
    }

    public void Initialize(AIEntity owner, Blackboard board)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _board = board ?? throw new ArgumentNullException(nameof(board));
        ResetFullCombat();
    }

    public void Reset()
    {
        ResetFullCombat();
    }

    public bool IsThreateningPlayer(Player player)
    {
        if (player == null) return false;
        if (_state.PhaseDefault != AiPhaseDefault.Engaged) return false;
        return false;
        
        return _board != null && _board.Target == player;
    }

    private void ResetFullCombat()
    {
        _state.PhaseDefault = AiPhaseDefault.Idle;
        _state.SuspicionRemaining = 0;
        _state.ChaseTurns = 0;
        _state.LastTrackedTarget = null;
        _board?.ClearTargetOnly();
    }

    public async UniTask ExecuteTurn(AiContext ctx)
    {
        var cfg = ctx.AiConfig;

        var baseParams = cfg?.AiParamsBase;
        var vision = baseParams?.VisionRange ?? 8;
        var aggro = baseParams?.AggroRange ?? 1;
        var threatTime = baseParams?.ThreatTime ?? 0;

        var chaseTired = 5;
        if (cfg?.StrategyParams is DefaultParams dp)
            chaseTired = dp.ChaseTiredDuration <= 0 ? int.MaxValue : dp.ChaseTiredDuration;

        var tree = new DefaultEnemyAiTreeContext(this, vision, aggro, threatTime, chaseTired);

        await tree.Selector(
            t => t.HandleReturningHome(),
            t => t.MainTree()
        );
    }

    private sealed class DefaultEnemyAiTreeContext
    {
        public DefaultEnemyAiTreeContext(DefaultAiStrategy strategy, int vision, int aggro, int threatTime,
            int chaseTired)
        {
            _s = strategy;
            Vision = vision;
            Aggro = aggro;
            ThreatTime = threatTime;
            ChaseTired = chaseTired;
        }

        private readonly DefaultAiStrategy _s;
        public int Vision { get; }
        public int Aggro { get; }
        public int ThreatTime { get; }
        public int ChaseTired { get; }

        public Blackboard Board => _s._board;
        public AIEntity Owner => _s._owner;

        public Entity Target { get; private set; }
        public int Dist { get; private set; }

        private DefaultState StateDefault => _s._state;

        /// <summary>回巢中：玩家回到脱离范围内则交回主流程；已到家则 Idle；否则向出生格走一步。</summary>
        public async UniTask<bool> HandleReturningHome()
        {
            if (!Owner.HasLeashConfigured || StateDefault.PhaseDefault != AiPhaseDefault.ReturningHome)
                return false;

            var player = FindPlayerInVision();
            if (player != null &&
                Owner.HomeGridPosition.Dist(player.GridPosition) <= Owner.DisengageLeashRange)
            {
                ResumeIdleForReEngage();
                return false;
            }

            if (Owner.GridPosition.Dist(Owner.HomeGridPosition) <= 0)
            {
                ResumeIdleForReEngage();
                return true;
            }

            await Board.MoveTowardsGrid(Owner.HomeGridPosition);
            return true;
        }

        private void ResumeIdleForReEngage()
        {
            StateDefault.PhaseDefault = AiPhaseDefault.Idle;
            StateDefault.ChaseTurns = 0;
            StateDefault.SuspicionRemaining = 0;
            StateDefault.LastTrackedTarget = null;
            Board.ClearTargetOnly();
        }

        private Entity FindPlayerInVision()
        {
            if (!EntityManager.HasInstance())
                return null;
            var raw = EntityManager.Instance.FindEnemies(Owner, Vision);
            var list = raw ?? new List<Entity>();
            return list.Count > 0 ? list[0] : null;
        }

        public async UniTask<bool> MainTree()
        {
            return await this.Selector(
                t => t.RootNoEnemyResetIdle(),
                t => t.Sequencer(
                    t1 => t1.BindTarget(),
                    t2 => t2.ApplyPhaseEngagement(),
                    t3 => t3.EngagedLayer()
                )
            );
        }

        public UniTask<bool> RootNoEnemyResetIdle()
        {
            if (EntityManager.HasInstance())
            {
                var raw = EntityManager.Instance.FindEnemies(Owner, Vision);
                var enemies = raw ?? new List<Entity>();
                if (enemies.Count != 0)
                    return UniTask.FromResult(false);

                _s.ResetFullCombat();
                return UniTask.FromResult(true);    
            }
            return UniTask.FromResult(false);  
        }

        public UniTask<bool> BindTarget()
        {
            if (EntityManager.HasInstance())
            {
                var raw = EntityManager.Instance.FindEnemies(Owner, Vision);
                var enemies = raw ?? new List<Entity>();
                Target = enemies[0];
                Board.SetTarget(Target);
                Dist = Owner.GridPosition.Dist(Target.GridPosition);
                return UniTask.FromResult(true);
            }
            return UniTask.FromResult(false);
        }

        public UniTask<bool> ApplyPhaseEngagement()
        {
            if (Dist <= Aggro)
            {
                if (StateDefault.PhaseDefault != AiPhaseDefault.Engaged)
                    StateDefault.ChaseTurns = 0;
                StateDefault.PhaseDefault = AiPhaseDefault.Engaged;
                StateDefault.SuspicionRemaining = 0;
                return UniTask.FromResult(true);
            }

            if (StateDefault.PhaseDefault == AiPhaseDefault.Engaged && Dist <= Vision)
                return UniTask.FromResult(true);

            if (Dist <= Vision)
            {
                if (ThreatTime == 0)
                {
                    if (StateDefault.PhaseDefault != AiPhaseDefault.Engaged)
                        StateDefault.ChaseTurns = 0;
                    StateDefault.PhaseDefault = AiPhaseDefault.Engaged;
                    return UniTask.FromResult(true);
                }

                if (StateDefault.LastTrackedTarget != Target)
                {
                    StateDefault.SuspicionRemaining = ThreatTime;
                    StateDefault.LastTrackedTarget = Target;
                }

                if (StateDefault.SuspicionRemaining > 0)
                {
                    StateDefault.SuspicionRemaining--;
                    if (StateDefault.SuspicionRemaining > 0)
                    {
                        StateDefault.PhaseDefault = AiPhaseDefault.Suspicious;
                        return UniTask.FromResult(false);
                    }
                }

                if (StateDefault.PhaseDefault != AiPhaseDefault.Engaged)
                    StateDefault.ChaseTurns = 0;
                StateDefault.PhaseDefault = AiPhaseDefault.Engaged;
                return UniTask.FromResult(true);
            }

            _s.ResetFullCombat();
            return UniTask.FromResult(false);
        }

        private UniTask<bool> EngagedLayer()
        {
            return this.Selector(
                t => t.ChaseTiredReset(),
                t => t.CombatAndCountTurn()
            );
        }

        private UniTask<bool> ChaseTiredReset()
        {
            if (StateDefault.ChaseTurns < ChaseTired)
                return UniTask.FromResult(false);

            if (Owner.HasLeashConfigured)
            {
                var player = Board.Target ?? FindPlayerInVision();
                if (player != null)
                {
                    var d = Owner.HomeGridPosition.Dist(player.GridPosition);
                    if (d > Owner.DisengageLeashRange)
                    {
                        StateDefault.PhaseDefault = AiPhaseDefault.ReturningHome;
                        StateDefault.SuspicionRemaining = 0;
                        Board.ClearTargetOnly();
                        return UniTask.FromResult(true);
                    }
                }
            }

            _s.ResetFullCombat();
            return UniTask.FromResult(true);
        }

        private async UniTask<bool> CombatAndCountTurn()
        {
            var range = Vision;
            await Board.Sequencer(
                b => b.FindTarget(range),
                b => b.Selector(
                    b1 => b1.If(
                        b2 => b2.SelectAbility(),
                        b3 => b3.UseAbility()
                    ),
                    b4 => b4.Follow()
                )
            );
            StateDefault.ChaseTurns++;
            return true;
        }
    }
}
