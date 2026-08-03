/*
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 任务阶段，包含若干多态 <see cref="QuestGoal"/>。
/// <see cref="SerializeReference"/> 供 Inspector 编辑模板资源；存档 JSON 中 <see cref="QuestGoal"/> 多态由 Newtonsoft 写入类型信息。
/// </summary>
[Serializable]
public class QuestPhase
{
    public string phaseId = "";

    [SerializeReference]
    public List<QuestGoal> goals = new();

    public bool AllGoalsComplete =>
        goals == null || goals.Count == 0 || goals.All(g => g != null && g.IsCompleted);

    public void NormalizeAfterDeserialize()
    {
        goals ??= new List<QuestGoal>();
    }

    /// <summary>
    /// 由 <see cref="QuestGoal.OnComplete"/> 调用：若当前阶段全部目标已完成且为任务当前阶段，则驱动任务推进/完成。
    /// </summary>
    internal void OnGoalNotifiedComplete(QuestGoal source, Quest ownerQuest)
    {
        if (source == null || ownerQuest == null) return;
        if (!ownerQuest.IsAccepted || ownerQuest.IsCompleted) return;
        if (!ReferenceEquals(ownerQuest.CurrentPhase, this)) return;
        if (!AllGoalsComplete) return;
        ownerQuest.AdvanceWhenCurrentPhaseAllGoalsComplete();
    }
}
*/
