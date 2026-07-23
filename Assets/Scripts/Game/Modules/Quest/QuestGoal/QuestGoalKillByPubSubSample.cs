using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 示例：通过 PubSub 监听 <see cref="QuestEnemyKilledSampleEvent"/> 累积计数。请在任务资源上勾选 <see cref="Quest.autoBindPubSubGoalListeners"/>，或自行在适当时机调用 <see cref="BindListening"/>。
/// </summary>
[Serializable]
public class QuestGoalKillByPubSubSample : QuestGoal
{
    public int enemyTypeId;
    public int required;
    public int current;

    [NonSerialized]
    private Action _unsubscribe;

    public override bool IsCompleted => required <= 0 || current >= required;

    public override void BindListening(PubSub bus, Quest owner)
    {
        UnbindListening(bus);
        if (bus == null) return;

        _unsubscribe = bus.Subscribe<QuestEnemyKilledSampleEvent>(evt =>
        {
            if (evt != null && evt.EnemyTypeId == enemyTypeId && current < required && required > 0)
            {
                current++;
                if (IsCompleted)
                    OnComplete();
            }

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
