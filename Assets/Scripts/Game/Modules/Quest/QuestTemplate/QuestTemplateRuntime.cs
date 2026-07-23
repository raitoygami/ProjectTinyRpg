/// <summary>
/// 将任务整理为「模板 / 新注册用」状态：未接取、进度清零。
/// </summary>
public static class QuestTemplateRuntime
{
    public static void ApplyTemplateRuntimeDefaults(Quest quest)
    {
        if (quest == null) return;
        quest.acceptedMinutes = -1;
        quest.completedMinutes = -1;
        quest.currentPhaseIndex = 0;
        quest.NormalizeAfterDeserialize();
        if (quest.phases == null) return;
        foreach (var p in quest.phases)
        {
            p?.NormalizeAfterDeserialize();
            if (p?.goals == null) continue;
            foreach (var g in p.goals)
                g?.Reset();
        }
    }
}
