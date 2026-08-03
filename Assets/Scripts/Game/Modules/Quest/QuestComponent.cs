/*
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 维护与对话条件相关的任务全局 Bool（接取 / 完成），通过 <see cref="Context.Instance"/>.Messager 订阅
/// <see cref="QuestAccepted"/>、<see cref="QuestComplete"/>、<see cref="QuestPhaseComplete"/> 同步更新。
/// </summary>
public class QuestComponent : MonoBehaviour
{
    public const string CompleteParameterSuffix = "_Complete";

    [SerializeField] private List<string> _questIdsForDialogueBool = new();
    [SerializeField] private List<string> _questPhaseIds = new();

    private void Awake()
    {
        this.SubscribeGlobal<QuestAccepted>(OnQuestAccepted);
        this.SubscribeGlobal<QuestComplete>(OnQuestComplete);
        this.SubscribeGlobal<QuestPhaseComplete>(OnQuestPhaseComplete);
    }

    private void Start()
    {
        RefreshQuestBools();
    }

    /// <summary>按 Inspector 列表刷新全部任务与阶段相关全局参数。</summary>
    public void RefreshQuestBools()
    {
        RefreshQuestParameters();
        RefreshQuestPhaseParameters();
    }

    /// <summary>仅按任务 id 列表刷新任务接取/完成 Bool。</summary>
    public void RefreshQuestParameters()
    {
        if (!Context.HasInstance() || _questIdsForDialogueBool == null) return;
        foreach (var questId in _questIdsForDialogueBool)
        {
            if (string.IsNullOrEmpty(questId)) continue;
            RefreshQuestParameter(questId);
        }
    }

    /// <summary>仅按阶段 id 列表刷新阶段完成 Bool。</summary>
    public void RefreshQuestPhaseParameters()
    {
        if (!Context.HasInstance() || _questPhaseIds == null) return;
        foreach (var phaseId in _questPhaseIds)
        {
            if (string.IsNullOrEmpty(phaseId)) continue;
            RefreshQuestPhaseParameter(phaseId);
        }
    }

    /// <summary>更新指定 <paramref name="questId"/> 的全局参数（是否接取、是否整任务完成）。</summary>
    public void RefreshQuestParameter(string questId)
    {
        if (!Context.HasInstance() || string.IsNullOrEmpty(questId)) return;
        var global = Context.Instance.GlobalParameters;
        if (!QuestManager.HasInstance() || !QuestManager.Instance.TryGetQuest(questId, out var q))
        {
            global.SetBool(questId, false);
            global.SetBool(questId + CompleteParameterSuffix, false);
            return;
        }

        global.SetBool(questId, q.IsAccepted);
        global.SetBool(questId + CompleteParameterSuffix, q.IsCompleted);
    }

    /// <summary>更新指定 <paramref name="phaseId"/> 的全局参数（该阶段是否已达成）。</summary>
    public void RefreshQuestPhaseParameter(string phaseId)
    {
        if (!Context.HasInstance() || string.IsNullOrEmpty(phaseId)) return;
        var global = Context.Instance.GlobalParameters;
        if (!QuestManager.HasInstance() || !TryFindQuestPhase(phaseId, out var q, out var phaseIndex))
        {
            global.SetBool(phaseId, false);
            return;
        }

        global.SetBool(phaseId, IsPhaseCompleted(q, phaseIndex));
    }

    /// <summary>仅刷新列表中属于 <paramref name="questId"/> 的阶段项。</summary>
    public void RefreshQuestPhaseParametersForQuest(string questId)
    {
        if (!Context.HasInstance() || string.IsNullOrEmpty(questId) || _questPhaseIds == null) return;
        foreach (var phaseId in _questPhaseIds)
        {
            if (string.IsNullOrEmpty(phaseId)) continue;
            if (!TryFindQuestPhase(phaseId, out var q, out _) || q.questId != questId) continue;
            RefreshQuestPhaseParameter(phaseId);
        }
    }

    private UniTask OnQuestAccepted(QuestAccepted e)
    {
        if (e == null || string.IsNullOrEmpty(e.QuestId) ||
            (!IsQuestIdTracked(e.QuestId) && !HasPhaseTrackedForQuest(e.QuestId)))
            return UniTask.CompletedTask;
        RefreshQuestBools();
        return UniTask.CompletedTask;
    }

    private UniTask OnQuestComplete(QuestComplete e)
    {
        if (e == null || string.IsNullOrEmpty(e.QuestId) ||
            (!IsQuestIdTracked(e.QuestId) && !HasPhaseTrackedForQuest(e.QuestId)))
            return UniTask.CompletedTask;
        RefreshQuestBools();
        return UniTask.CompletedTask;
    }

    private UniTask OnQuestPhaseComplete(QuestPhaseComplete e)
    {
        if (e == null || string.IsNullOrEmpty(e.QuestId) ||
            (!IsQuestIdTracked(e.QuestId) && !IsPhaseIdTracked(e.PhaseId)))
            return UniTask.CompletedTask;
        RefreshQuestBools();
        return UniTask.CompletedTask;
    }

    private bool IsQuestIdTracked(string questId)
    {
        if (_questIdsForDialogueBool == null) return false;
        foreach (var id in _questIdsForDialogueBool)
        {
            if (id == questId) return true;
        }

        return false;
    }

    private bool IsPhaseIdTracked(string phaseId)
    {
        if (string.IsNullOrEmpty(phaseId) || _questPhaseIds == null) return false;
        foreach (var id in _questPhaseIds)
        {
            if (id == phaseId) return true;
        }

        return false;
    }

    private bool HasPhaseTrackedForQuest(string questId)
    {
        if (string.IsNullOrEmpty(questId) || _questPhaseIds == null) return false;
        foreach (var phaseId in _questPhaseIds)
        {
            if (string.IsNullOrEmpty(phaseId)) continue;
            if (TryFindQuestPhase(phaseId, out var q, out _) && q.questId == questId)
                return true;
        }

        return false;
    }

    private static bool TryFindQuestPhase(string phaseId, out Quest quest, out int phaseIndex)
    {
        quest = null;
        phaseIndex = -1;
        if (string.IsNullOrEmpty(phaseId) || !QuestManager.HasInstance()) return false;
        foreach (var q in QuestManager.Instance.AllQuests)
        {
            if (q?.phases == null) continue;
            for (var i = 0; i < q.phases.Count; i++)
            {
                var p = q.phases[i];
                if (p != null && p.phaseId == phaseId)
                {
                    quest = q;
                    phaseIndex = i;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsPhaseCompleted(Quest q, int phaseIndex)
    {
        if (q == null || q.phases == null || phaseIndex < 0 || phaseIndex >= q.phases.Count)
            return false;
        if (q.IsCompleted) return true;
        if (!q.IsAccepted) return false;
        if (q.currentPhaseIndex > phaseIndex) return true;
        if (q.currentPhaseIndex < phaseIndex) return false;
        var phase = q.phases[phaseIndex];
        return phase != null && phase.AllGoalsComplete;
    }
}
*/
