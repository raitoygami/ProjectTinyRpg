using System;
using System.Collections.Generic;
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
[AIStrategy(AIPattern.Default)]
public sealed class AIStrategyDefault : IAIStrategy
{
    private Blackboard _board;

    private AIEntity Owner { get; set; }

    public void Initialize(AIEntity owner, Blackboard board)
    {
        Owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _board = board ?? throw new ArgumentNullException(nameof(board));

        ResetFullCombat();
    }

    public void Reset()
    {
        ResetFullCombat();
    }

    private void ResetFullCombat()
    {
        _board?.ClearTargetOnly();
        CombatManager.Instance.RemoveEnemyTarget(Owner);
    }

    private DefaultEnemyAiTreeContext _treeContext;

    public async UniTask ExecuteTurn(AiContext ctx)
    {
        if (!EntityManager.HasInstance())
            return;

        var parameter = ctx.Parameter as AIParameterDefault;

        var vision = parameter?.VisionRange ?? 5;
        var threatTime = parameter?.ThreatTime ?? 0;

        _treeContext ??= new DefaultEnemyAiTreeContext(this, vision, threatTime, 0);

        await _treeContext.Selector(
            t => t.HandleReturningHome(),
            t => t.MainTree()
        );
    }

    private sealed class DefaultEnemyAiTreeContext
    {
        

        public DefaultEnemyAiTreeContext(AIStrategyDefault strategy, int vision, int threatTime,
            int chaseTired)
        {
            _s = strategy;
            Vision = vision;
            ThreatTime = threatTime;
            ChaseTired = chaseTired;
        }

        private readonly AIStrategyDefault _s;
        public int Vision { get; }
        public int ThreatTime { get; }
        public int ChaseTired { get; }

        public Blackboard Board => _s._board;
        public AIEntity Owner => _s.Owner;

        public Entity Target { get; private set; }
        public int Dist { get; private set; }

        private bool _IsReturningHome;

        /// <summary>回巢中：玩家回到脱离范围内则交回主流程；已到家则 Idle；否则向出生格走一步。</summary>
        /// // 往回走的时候就不在索敌，一直到回到家
        public async UniTask<bool> HandleReturningHome()
        {
            if (!Owner.HasLeashConfigured)
                return false;
            // 如果当前目标不为空

            if (Target == null)
            {
                var enemies = EntityManager.Instance.FindEnemies(Owner, Vision);
                Target = GetTargetableEntity(enemies);
            }

            if (Target != null)
            {
                var dist = Target.GridPosition.Dist(Owner.SpawnPointLocation);
                // 没有往回走的时候，且玩家还没走出两倍的范围
                if (!_IsReturningHome)
                {
                    if (dist < Owner.DisengageLeashRange * 3)
                        return false;
                }
                else
                {
                    dist = Target.GridPosition.Dist(Owner.SpawnLocation);
                    // 当怪物正在往回走
                    // 当玩家重新进入刷怪点切还没有从从怪物锁定上移除的时候
                    if (dist < Owner.DisengageLeashRange)
                    {
                        _IsReturningHome = false;
                        return false;
                    }
                }
            }

            if (Owner.GridPosition.Dist(Owner.SpawnLocation) <= 0)
            {
                OnBackToSpawnerPoint();
                return false;
            }

            CombatManager.Instance.RemoveEnemyTarget(Owner);
            await Board.MoveTowardsGrid(Owner.SpawnLocation);
            _IsReturningHome = true;

            return true;
        }

        // 回到出生点以后, 将target清空
        private void OnBackToSpawnerPoint()
        {
            Target = null;
            _IsReturningHome = false;
            Board.ClearTargetOnly();
        }

        public UniTask<bool> FindTarget(int range)
        {
            if (!EntityManager.HasInstance()) return UniTask.FromResult(false);
            // 如果当前目标还在,切可以被锁定
            if (Target != null && Target.GetComponent<AgentStats>().Targetable())
            {
                Board.SetTarget(Target);
                Dist = Owner.GridPosition.Dist(Target.GridPosition);
                CombatManager.Instance.AddEnemyTarget(Owner);
                return UniTask.FromResult(true);
            }

            var enemies = EntityManager.Instance.FindEnemies(Owner, range);
            if (enemies == null || enemies.Count == 0)
                return UniTask.FromResult(false);
            var target = GetTargetableEntity(enemies);
            if (target == null)
                return UniTask.FromResult(false);

            Target = target;
            Board.SetTarget(Target);
            Dist = Owner.GridPosition.Dist(Target.GridPosition);
            CombatManager.Instance.AddEnemyTarget(Owner);
            return UniTask.FromResult(true);
        }

        // 从事业范围里找能看到的敌人
        private Entity GetTargetableEntity(List<Entity> enemies)
        {
            var start = Owner.GridPosition;
            foreach (var e in enemies)
            {
                if (!e.GetComponent<AgentStats>().Targetable()) continue;

                var target = e.GridPosition;
                var line = start.Line(target);
                var block = false;
                for (var i = 1; i < line.Count; i++)
                {
                    var node = PathFinder.Instance.GetCell(line[i].x, line[i].y);
                    if (node?.Logical == null) continue;

                    if (node.Logical.BlockVision() || (Const.Layer.ObstacleOnly.value & node.Logical.Layer.value) != 0)
                    {
                        block = true;
                        break;
                    }
                }

                if (!block)
                    return e;
            }

            return null;
        }

        public async UniTask<bool> MainTree()
        {
            await Board.Selector(
                // 攻击分支：先选能力，再根据准备状态决定动作
                b => b.Sequencer(
                    _ => FindTarget(Vision), // 寻找目标
                    b2 => b2.If(b3 => b3.SelectAbility(), // 选择能力（假定返回 bool 表示成功）)
                        b4 => b4.Selector( // 根据是否准备选择执行 Prepare 或 UseAbility
                            b5 => b5.If(b6 => b6.IsPreparing()
                                , b7 => b7.Prepare()
                                ),
                            b8 => b8.UseAbility()
                        )
                    )
                )
                // 如果攻击分支失败（找不到目标、选能力失败、动作失败），则跟随
                , b9 => b9.Follow());

            return true;
        }
    }
}