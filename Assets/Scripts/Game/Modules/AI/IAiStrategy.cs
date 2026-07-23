using Cysharp.Threading.Tasks;

/// <summary>
/// 单个 AI 实体上的策略实例：运行时状态由策略自身维护；载体 <see cref="AIEntity"/> 只负责
/// <see cref="Initialize"/> 与 <see cref="Reset"/>，不持有统一 AIState。
/// </summary>
public interface IAiStrategy
{
    /// <summary>在载体上绑定黑板后调用一次（如 <see cref="AIEntity.ConfigureAsEnemy"/> 流程内）。</summary>
    void Initialize(AIEntity owner, Blackboard board);

    /// <summary>清空本策略的运行时状态（如目标、阶段）；载体在需要强制重置 AI 时调用。</summary>
    void Reset();

    UniTask ExecuteTurn(AiContext ctx);

    /// <summary>是否处于对玩家的有效威胁（用于接战判定等）。</summary>
    bool IsThreateningPlayer(Player player);
}
