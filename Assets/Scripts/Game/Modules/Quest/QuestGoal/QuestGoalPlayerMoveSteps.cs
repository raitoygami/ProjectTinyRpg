using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 统计主角色移动步数：在玩家 <see cref="Entity"/> 的 <see cref="PubSubActor.Messager"/> 上监听
/// <see cref="AgentMover.MoveStartEvent"/>（每开始移动一格计一次）。
/// 请在任务资源上勾选 <see cref="Quest.autoBindPubSubGoalListeners"/>，或在接取后自行调用 <see cref="BindListening"/>；
/// 读档后若玩家已就绪，请对进行中任务调用 <see cref="Quest.RebindPubSubGoalListeners"/>。
/// </summary>
[Serializable]
public class QuestGoalPlayerMoveSteps : QuestGoal
{
    public int required;
    public int current;

    [NonSerialized]
    private Action _unsubscribe;

    public override bool IsCompleted => required <= 0 || current >= required;

    public override void BindListening(PubSub bus, Quest owner)
    {
        UnbindListening(bus);
        if (!Context.HasInstance()) return;
        var player = Context.Instance.PlayerInst;
        if (player == null) return;

        _unsubscribe = player.Messager.Subscribe<AgentMover.MoveStartEvent>(_ =>
        {
            if (required <= 0 || IsCompleted) return UniTask.CompletedTask;
            current = Math.Min(current + 1, required);
            if (IsCompleted)
                OnComplete();
            return UniTask.CompletedTask;
        });
    }

    public override void UnbindListening(PubSub bus)
    {
        _unsubscribe?.Invoke();
        _unsubscribe = null;
    }

    public override void Reset()
    {
        current = 0;
    }
}
