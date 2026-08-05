using System;
using System.Collections.Generic;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

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

    public AIEntity Owner => _owner;
    
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

    private void ResetFullCombat()
    {
        _board?.ClearTargetOnly();
        BattleManager.Instance.RemoveEnemyTarget(_owner);
    }

    private DefaultEnemyAiTreeContext _treeContext;
    
    public async UniTask ExecuteTurn(AiContext ctx)
    {
        var cfg = ctx.AiConfig;

        var baseParams = cfg?.AiParamsBase;
        var vision = baseParams?.VisionRange ?? 8;
        var aggro = baseParams?.AggroRange ?? 1;
        var threatTime = baseParams?.ThreatTime ?? 0;

 
        _treeContext ??= new DefaultEnemyAiTreeContext(this, vision, aggro, threatTime, 0);

        await _treeContext.Selector(
            t => t.HandleReturningHome(),
            t => t.MainTree()
        );
    }

    private sealed class DefaultEnemyAiTreeContext
    {
        private Player.EnterCombatEvt OnEnterCombatEvt;
        
        public DefaultEnemyAiTreeContext(DefaultAiStrategy strategy, int vision, int aggro, int threatTime,
            int chaseTired)
        {
            _s = strategy;
            Vision = vision;
            Aggro = aggro;
            ThreatTime = threatTime;
            ChaseTired = chaseTired;
            OnEnterCombatEvt = new Player.EnterCombatEvt();
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

        private bool _IsReturningHome = false;
        
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
                var dist = Target.GridPosition.Dist(Owner.SpawnLocation);
                // 没有往回走的时候，且玩家还没走出两倍的范围
                if (!_IsReturningHome)
                {
                    if (dist < Owner.DisengageLeashRange * 2)
                        return false;
                }
                else
                {
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
            
            BattleManager.Instance.RemoveEnemyTarget(Owner);
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
                Owner.PublishGlobal(OnEnterCombatEvt);
                BattleManager.Instance.AddEnemyTarget(Owner);
                return UniTask.FromResult(true);
            }
            var enemies = EntityManager.Instance.FindEnemies(Owner, range);
            if (enemies == null || enemies.Count == 0)
                return UniTask.FromResult(false);
            var target =  GetTargetableEntity(enemies);
            if (target == null)
                return UniTask.FromResult(false);
            Debug.Log($"{Owner.name}-{Owner.GridPosition.x},{Owner.GridPosition.y}");
            foreach (var position in Owner.GridPosition.LineTo(target.GridPosition))
            {
                var node = PathFinder.Instance.GetNode(position.x, position.y);
                if (node?.Logical != null)
                {
                    var layerIndex = Mathf.RoundToInt(Mathf.Log(node.Logical.Layer, 2));
                    Debug.Log($"{LayerMask.LayerToName(layerIndex)}-{node.Logical.BlockVision()}");
                }
            }
            Target = target;
            Board.SetTarget(Target);
            Dist = Owner.GridPosition.Dist(Target.GridPosition);
            Owner.PublishGlobal(OnEnterCombatEvt);
            BattleManager.Instance.AddEnemyTarget(Owner);
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
                var line = start.LineTo(target);
                var block = false;
                for (var i = 1; i < line.Count; i++)
                {
                    var node = PathFinder.Instance.GetNode(line[i].x, line[i].y);
                    if (node?.Logical == null ) continue;

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
            await Board.Sequencer(
                _ => FindTarget(Vision),
                b => b.Selector(
                    b1 => b1.If(
                        b2 => b2.SelectAbility(),
                        b3 => b3.UseAbility()
                    ),
                    b4 => b4.Follow()
                )
            );
            return true;
        }
    }
}
