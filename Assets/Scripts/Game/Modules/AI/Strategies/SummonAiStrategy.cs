using System;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 召唤物 AI：未发现敌对单位时在 <see cref="PlayerSummonParams.FollowDistance"/>（格）内跟随
/// <see cref="AIEntity.SummonOwner"/>（由 <see cref="AIEntity.ConfigureAsSummon"/> / 召唤流程显式设置）；
/// 跟随阶段不把玩家判为被本实体威胁，避免 <see cref="Player.RefreshCombatState"/> 进入接战而清掉玩家预存路径。
/// 视野内出现敌对单位时走黑板战斗（寻敌 / 技能 / 追击）。
/// </summary>
[AiStrategy(AiPattern.Summon)]
public sealed class SummonAiStrategy : IAiStrategy
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

    /// <summary>召唤物不“威胁”玩家本人；接战与清路径由敌方 <see cref="AIEntity"/> 的判定负责。</summary>
    public bool IsThreateningPlayer(Player player) => false;

    public async UniTask ExecuteTurn(AiContext ctx)
    {
        var cfg = ctx.AiConfig;
        var baseParams = cfg?.AiParamsBase;
        var vision = baseParams?.VisionRange ?? 8;

        var hostiles = EntityManager.Instance.FindEnemies(_owner, vision);
        if (hostiles != null && hostiles.Count > 0)
        {
            await RunCombat(vision);
            return;
        }

        await FollowOwner(ctx);
    }

    private async UniTask RunCombat(int visionRange)
    {
        await _board.Sequencer(
            b => b.FindTarget(visionRange),
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

        var followDist = ctx.AiConfig?.StrategyParams is Summon1Params p
            ? Mathf.Max(0, p.FollowDistance)
            : 2;

        var d = _owner.GridPosition.Dist(master.GridPosition);
        if (d <= followDist)
            return;

        await _board.MoveTowardsGrid(master.GridPosition);
    }
}
