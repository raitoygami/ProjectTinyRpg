using System;

/// <summary>
/// 对话 UI 正常播完结束动画后，由 <see cref="UIDialogue"/> 经全局 PubSub 广播（仅当对应 <see cref="Dialogue"/> 配置了非空 uid）。
/// </summary>
public class DialogueCompletedEvent : EventArgs
{
    public string DialogueUid;
}

/// <summary>
/// 对话等 UI 确认接取任务后，由 <see cref="UIDialogue"/> 经全局 PubSub 广播。
/// </summary>
public class QuestAcceptedEvent : EventArgs
{
    public string QuestId;
}

/// <summary>
/// 任务在 <see cref="Quest.TryAccept"/> 成功接取后，由 <see cref="Context.Instance"/>.Messager 广播。
/// </summary>
public class QuestAccepted : EventArgs
{
    public string QuestId;
}

/// <summary>
/// 任务在 <see cref="Quest.TryComplete"/> 成功完成时，由 <see cref="Context.Instance"/>.Messager 广播。
/// </summary>
public class QuestComplete : EventArgs
{
    public string QuestId;
}

/// <summary>
/// 某一阶段目标全部达成后：由非最后一阶段推进时、或整任务在 <see cref="Quest.TryComplete"/> 完成最后一阶段时，
/// 由 <see cref="Context.Instance"/>.Messager 广播（先于 <see cref="QuestPhaseAdvancedEvent"/> / <see cref="QuestComplete"/>）。
/// </summary>
public class QuestPhaseComplete : EventArgs
{
    public string QuestId;
    public int PhaseIndex;
    public string PhaseId;
}