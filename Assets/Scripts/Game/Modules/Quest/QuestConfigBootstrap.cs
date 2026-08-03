/*using UnityEngine;

/// <summary>
///     运行时根据 Inspector 中拖入的 <see cref="QuestTemplateAsset" /> 克隆任务并 <see cref="QuestManager.Register" />。
/// </summary>
public class QuestConfigBootstrap : MonoBehaviour
{
    [Tooltip("启动时依次从这些模板克隆 Quest 并注册（同 questId 已存在则跳过）")] [SerializeField]
    private QuestTemplateAsset[] questTemplates;

    [SerializeField] private bool registerOnStart = true;

    private void Start()
    {
        if (registerOnStart)
            RegisterAllFromTemplates();
    }

    /// <summary>
    ///     注册 <see cref="questTemplates" /> 中全部非空模板。
    /// </summary>
    public void RegisterAllFromTemplates()
    {
        if (questTemplates == null) return;

        foreach (var template in questTemplates)
        {
            if (template == null) continue;
            if (!TryRegisterFromTemplate(template, true))
                Debug.LogWarning($"[QuestConfigBootstrap] 跳过或失败: {template.name}", template);
        }
    }

    /// <summary>
    ///     从单个模板克隆并注册；返回是否新注册成功。
    /// </summary>
    public static bool TryRegisterFromTemplate(QuestTemplateAsset template, bool skipIfQuestIdExists = true)
    {
        if (template == null) return false;
        if (!Persist.HasInstance())
        {
            Debug.LogError("[QuestConfigBootstrap] 需要场景中存在 PersistenceModule。");
            return false;
        }

        if (!QuestManager.HasInstance())
        {
            Debug.LogError("[QuestConfigBootstrap] 需要场景中存在 QuestManager。");
            return false;
        }

        var templateQuest = template.quest;
        var templateQuestId = templateQuest?.questId;
        if (skipIfQuestIdExists && !string.IsNullOrEmpty(templateQuestId) &&
            QuestManager.Instance.TryGetQuest(templateQuestId, out _))
            return false;

        var q = template.InstantiateRuntimeQuest();
        if (q == null || string.IsNullOrEmpty(q.questId))
        {
            Debug.LogError($"[QuestConfigBootstrap] 模板「{template.name}」克隆失败或 questId 为空。", template);
            return false;
        }

        if (skipIfQuestIdExists && QuestManager.Instance.TryGetQuest(q.questId, out _))
            return false;

        QuestManager.Instance.Register(q);
        return true;
    }
}*/