using System;
using System.Linq;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;

[AIStrategy(AIPattern.Summon)]
public sealed class AIStrategySummon : IAIStrategy
{
    private AIEntity _owner;
    private Blackboard _board;

    public void Initialize(AIEntity owner, Blackboard board)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _board = board ?? throw new ArgumentNullException(nameof(board));
        Reset();
    }

    public void Reset()
    {
        _board?.ClearTargetOnly();
    }

    public async UniTask ExecuteTurn(AiContext ctx)
    {
        var cfg = ctx.Parameter as AIParameterSummon;
        var vision = cfg?.VisionRange ?? 8;

        var hostiles = EntityManager.Instance.FindEnemies(_owner, vision);
        if (hostiles is { Count: > 0 })
        {
            await RunCombat(vision);
            return;
        }

        await FollowOwner(ctx);
    }

    private UniTask<bool> FindTarget(int range)
    {
        var enemies = EntityManager.Instance.FindEnemies(_owner, range);
        if (enemies == null || enemies.Count == 0)
            return UniTask.FromResult(false);

        var _target = enemies.FirstOrDefault();
        _board?.SetTarget(_target);
        return UniTask.FromResult(true);
    }
    
    private async UniTask RunCombat(int visionRange)
    {
        await _board.Sequencer(
            _ => FindTarget(visionRange),
            b => b.Selector(
                b1 => b1.If(
                    b2 => b2.SelectAbility(),
                    b3 => b3.UseAbility()
                ),
                b4 => b4.Follow()
            )
        );
    }

    private async UniTask FollowOwner(AiContext ctx)
    {
        var master = _owner.SummonOwner;
        if (master == null)
            return;

        var followDist = ctx.Parameter is AIParameterSummon p
            ? Mathf.Max(0, p.FollowDistance)
            : 2;

        var d = _owner.GridPosition.Dist(master.GridPosition);
        if (d <= followDist)
            return;

        await _board.MoveTowardsGrid(master.GridPosition);
    }
}
