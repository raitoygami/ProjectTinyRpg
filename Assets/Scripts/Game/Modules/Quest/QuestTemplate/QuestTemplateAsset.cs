/*
using UnityEngine;

/// <summary>
/// 任务模板（ScriptableObject）：在 Inspector 中编辑 <see cref="Quest"/> / <see cref="QuestGoal"/>。
/// 运行时 <see cref="InstantiateRuntimeQuest"/> 经 <see cref="QuestDeepClone"/> 克隆后由 <see cref="QuestManager.TryRegisterFromTemplate"/> / <see cref="QuestManager.Register"/> 注册。
/// </summary>
[CreateAssetMenu(fileName = "QuestTemplate", menuName = "Quest/Quest Template", order = 0)]
public class QuestTemplateAsset : ScriptableObject
{
    [SerializeReference]
    public Quest quest;

    /// <summary>
    /// 从本资源克隆一条独立的 <see cref="Quest"/> 供运行时注册；不修改资源上的数据。
    /// </summary>
    public Quest InstantiateRuntimeQuest()
    {
        if (quest == null) return null;

        Quest copy = QuestDeepClone.CloneQuest(quest);
        if (copy == null) return null;

        QuestTemplateRuntime.ApplyTemplateRuntimeDefaults(copy);
        return copy;
    }
}
*/
