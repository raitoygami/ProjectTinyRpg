/*
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 当其它指定任务整任务完成（<see cref="QuestComplete"/>）时达成本目标。接取时若该任务已完成则立即达成。请勾选 <see cref="Quest.autoBindPubSubGoalListeners"/>。
/// </summary>
[Serializable]
public class QuestQuestComplete : QuestGoal
{
    [Tooltip("要等待完成的其它任务的 questId。")]
    public string targetQuestId = "";

    [NonSerialized]
    bool _completed;

    [NonSerialized]
    Action _unsubscribe;

    public override bool IsCompleted => _completed;

    public override void BindListening(PubSub bus, Quest owner)
    {
        UnbindListening(bus);
        if (string.IsNullOrEmpty(targetQuestId)) return;

        if (QuestManager.HasInstance() && QuestManager.Instance.TryGetQuest(targetQuestId, out var other) &&
            other.IsCompleted)
        {
            _completed = true;
            OnComplete();
            return;
        }

        if (bus == null) return;

        _unsubscribe = bus.Subscribe<QuestComplete>(evt =>
        {
            if (evt != null && evt.QuestId == targetQuestId && !_completed)
            {
                _completed = true;
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
        _completed = false;
    }
}
*/
