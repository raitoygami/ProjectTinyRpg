/*
#if UNITY_EDITOR
using System.Collections.Generic;

/// <summary>
/// 编辑器内构造示例任务图（不进入包体 unless referenced）。
/// </summary>
public static class QuestEditorSampleData
{
    public static Quest CreateSampleQuest()
    {
        var quest = new Quest
        {
            questId = "sample_config_main",
            type = QuestType.Main,
            repeatable = false,
        };

        var phase1 = new QuestPhase
        {
            phaseId = "kill_slimes",
            goals = new List<QuestGoal>
            {
                new QuestGoalCounter { goalId = "kill", current = 0, required = 3 },
            },
        };
        quest.AddPhase(phase1);

        var phase2 = new QuestPhase
        {
            phaseId = "return_npc",
            goals = new List<QuestGoal>(),
        };
        quest.AddPhase(phase2);

        QuestTemplateRuntime.ApplyTemplateRuntimeDefaults(quest);
        return quest;
    }
}
#endif
*/
