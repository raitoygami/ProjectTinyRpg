/*
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 任务查询与注册；数据仅存在于 <see cref="GameData.quests"/>，依赖 <see cref="PersistenceModule"/>。
/// </summary>
public class QuestManager : Singleton<QuestManager>
{
    private static GameData RuntimeData =>
        PersistenceModule.HasInstance() ? PersistenceModule.Instance.GetRuntimeData() : null;

    public IEnumerable<Quest> AllQuests
    {
        get
        {
            var data = RuntimeData;
            if (data?.quests == null) yield break;
            foreach (var q in data.quests)
            {
                if (q != null)
                    yield return q;
            }
        }
    }

    public void Register(Quest quest)
    {
        if (quest == null) throw new ArgumentNullException(nameof(quest));
        var data = RuntimeData;
        if (data == null)
            throw new InvalidOperationException("QuestManager.Register 需要已存在的 PersistenceModule 与 GameData。");
        quest.NormalizeAfterDeserialize();
        if (!AreDependenciesCompleted(quest))
            throw new InvalidOperationException(
                $"Quest 「{quest.questId}」的依赖任务尚未全部完成，无法注册。");
        data.AddQuestIfMissing(quest);
    }

    /// <summary>
    /// 依赖列表中每个 questId 在存档中均存在且对应任务 <see cref="Quest.IsCompleted"/> 为 true 时返回 true；无依赖时返回 true。
    /// </summary>
    public bool AreDependenciesCompleted(Quest quest)
    {
        if (quest == null) return false;
        if (quest.dependencies == null || quest.dependencies.Count == 0) return true;
        foreach (var depId in quest.dependencies)
        {
            if (string.IsNullOrEmpty(depId)) continue;
            if (depId == quest.questId) continue;
            if (!TryGetQuest(depId, out var dep) || !dep.IsCompleted)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 用于对话显示条件：<paramref name="questId"/> 为空时不限制；否则需任务已存在、依赖已完成、且当前可接取（与 <see cref="Quest.TryAccept"/> 前置一致）。
    /// </summary>
    public bool IsQuestAcceptableForDialogue(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return true;
        if (!TryGetQuest(questId, out var q)) return false;
        if (!AreDependenciesCompleted(q)) return false;
        if (q.IsAccepted && !q.IsCompleted) return false;
        if (q.IsCompleted && !q.repeatable) return false;
        return true;
    }

    /// <summary>
    /// 从 <see cref="QuestTemplateAsset"/> 克隆一条 <see cref="Quest"/> 并 <see cref="Register"/>；返回是否新注册成功。
    /// 由外部在合适时机调用（不再由场景组件自动批量注册）。
    /// </summary>
    public bool TryRegisterFromTemplate(QuestTemplateAsset template, bool skipIfQuestIdExists = true)
    {
        if (template == null) return false;
        if (!PersistenceModule.HasInstance())
        {
            Debug.LogError("[QuestManager] 需要场景中存在 PersistenceModule。");
            return false;
        }

        var templateQuest = template.quest;
        var templateQuestId = templateQuest?.questId;
        if (skipIfQuestIdExists && !string.IsNullOrEmpty(templateQuestId) &&
            TryGetQuest(templateQuestId, out _))
            return false;

        var q = template.InstantiateRuntimeQuest();
        if (q == null || string.IsNullOrEmpty(q.questId))
        {
            Debug.LogError($"[QuestManager] 模板「{template.name}」克隆失败或 questId 为空。", template);
            return false;
        }

        if (skipIfQuestIdExists && TryGetQuest(q.questId, out _))
            return false;

        if (!AreDependenciesCompleted(q))
        {
            Debug.LogWarning($"[QuestManager] 模板「{template.name}」依赖未满足，跳过注册。", template);
            return false;
        }

        Register(q);
        return true;
    }

    public void Unregister(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return;
        RuntimeData?.RemoveQuest(questId);
    }

    public bool TryGetQuest(string questId, out Quest quest)
    {
        quest = null;
        if (string.IsNullOrEmpty(questId)) return false;
        var data = RuntimeData;
        return data != null && data.TryGetQuest(questId, out quest);
    }

    public IEnumerable<Quest> GetQuestsByType(QuestType type)
    {
        return AllQuests.Where(q => q.type == type);
    }

    public IEnumerable<Quest> GetActiveQuests()
    {
        return AllQuests.Where(q => q.IsAccepted && !q.IsCompleted);
    }

    public bool TryAcceptQuest(string questId)
    {
        if (!TryGetQuest(questId, out var quest))
        {
            Debug.LogWarning($"[QuestManager] Quest not found: {questId}");
            return false;
        }

        return quest.TryAccept();
    }

    public bool TryAdvanceOrCompleteQuest(string questId)
    {
        if (!TryGetQuest(questId, out var quest))
            return false;
        return quest.TryAdvanceOrComplete();
    }

    public bool TryCompleteQuest(string questId)
    {
        if (!TryGetQuest(questId, out var quest))
            return false;
        return quest.TryComplete();
    }
}
*/
