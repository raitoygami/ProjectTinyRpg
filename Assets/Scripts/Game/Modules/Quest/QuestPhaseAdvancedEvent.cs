using System;

/// <summary>
/// 任务在 <see cref="Quest.TryAdvanceOrComplete"/> 中从非最后一阶段推进到下一阶段时，由全局 <see cref="Context.Instance"/>.Messager 广播。
/// </summary>
public class QuestPhaseAdvancedEvent : EventArgs
{
    public string QuestId;
    public int PreviousPhaseIndex;
    public int NewPhaseIndex;
    public string PreviousPhaseId;
    public string NewPhaseId;
}
