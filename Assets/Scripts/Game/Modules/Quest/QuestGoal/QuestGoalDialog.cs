using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 完成指定 <see cref="Dialogue.uid"/> 的对话（正常播完结束动画）后达成目标。请在任务上勾选 <see cref="Quest.autoBindPubSubGoalListeners"/>。
/// </summary>
[Serializable]
public class QuestGoalDialog : QuestGoal
{
    [Tooltip("与 Dialogue 资源上的 uid 一致。")]
    public string dialogueUid = "";

    [NonSerialized]
    bool _completed;

    [NonSerialized]
    Action _unsubscribe;

    public override bool IsCompleted => _completed;

    public override void BindListening(PubSub bus, Quest owner)
    {
        UnbindListening(bus);
        if (bus == null || string.IsNullOrEmpty(dialogueUid)) return;

        _unsubscribe = bus.Subscribe<DialogueCompletedEvent>(evt =>
        {
            if (evt != null && evt.DialogueUid == dialogueUid && !_completed)
            {
                _completed = true;
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
        _completed = false;
    }
}
