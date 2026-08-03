/*
using System;

/// <summary>
/// 任务目标基类；具体进度判定由子类实现。存档中 <see cref="GameData.quests"/> 的多态由 Newtonsoft.Json（如 <c>$type</c>）保留。
/// 可选重写 <see cref="BindListening"/> / <see cref="UnbindListening"/> 订阅 <see cref="PubSub"/>。
/// 进度达成后调用 <see cref="OnComplete"/>，由 <see cref="QuestPhase"/> / <see cref="Quest"/> 判断阶段与整任务是否可推进。
/// </summary>
[Serializable]
public abstract class QuestGoal
{
    public string goalId = "";

    [NonSerialized]
    Quest _ownerQuest;

    [NonSerialized]
    QuestPhase _ownerPhase;

    public abstract bool IsCompleted { get; }

    internal void AttachContext(Quest quest, QuestPhase phase)
    {
        _ownerQuest = quest;
        _ownerPhase = phase;
    }

    /// <summary>
    /// 派生类在目标已满足 <see cref="IsCompleted"/> 后调用（应先更新自身字段再调用）。
    /// 将通知所属 <see cref="QuestPhase"/> 检查当前阶段全部目标；若完成且为任务当前阶段，则尝试 <see cref="Quest.TryAdvanceOrComplete"/>。
    /// </summary>
    protected void OnComplete()
    {
        if (!IsCompleted) return;
        _ownerPhase?.OnGoalNotifiedComplete(this, _ownerQuest);
    }

    /// <summary>
    /// 由派生类在合适时机调用（例如任务接受后）；<paramref name="bus"/> 一般为全局 <see cref="Context.Instance"/>.Messager。
    /// </summary>
    public virtual void BindListening(PubSub bus, Quest owner)
    {
    }

    /// <summary>
    /// 由派生类在合适时机调用（例如任务完成、重置进度、注销任务前）；应撤销 <see cref="BindListening"/> 里注册的订阅。
    /// </summary>
    public virtual void UnbindListening(PubSub bus)
    {
    }

    public virtual void Reset()
    {
    }
}
*/
