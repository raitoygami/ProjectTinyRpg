using System.Collections.Generic;

/// <summary>
/// 接任务节点：使用基类 <see cref="DialogueEntry.questId"/>；展示文案后，玩家在「下一句」时由 <see cref="UIDialogue"/> 调用 <see cref="QuestManager.TryAcceptQuest"/>。
/// </summary>
public class DialogueAcceptQuest : DialogueEntry
{
#if UNITY_EDITOR
    public override List<string> GetStyleClasses() {
        return new List<string> { "accept-quest" };
    }
#endif

    public override string GetDescription() {
        return string.IsNullOrEmpty(questId) ? "AcceptQuest" : $"AcceptQuest: {questId}";
    }
}
