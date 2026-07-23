using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
///     具体任务类型（存于 <see cref="GameData.quests" />）；阶段与目标在编辑器中组装。整份 <see cref="GameData" /> 存档由
///     <see cref="PersistenceModule" />（Newtonsoft.Json）持久化。
/// </summary>
[Serializable]
public sealed class Quest
{
    public string questId = "";
    public QuestType type;
    public bool repeatable;
    public int currentPhaseIndex;
    public int acceptedMinutes = -1;
    public int completedMinutes = -1;

    [Tooltip("勾选后：接取任务时自动对各 Goal 调用 BindListening，完成/重置时 Unbind（适用于 PubSub 类目标）。")]
    public bool autoBindPubSubGoalListeners;

    [Tooltip("依赖的前置任务 questId：列表中全部任务均为「已完成」后才允许注册与接取；默认可为空。")]
    public List<string> dependencies = new();

    public List<QuestPhase> phases = new();

    /// <summary>不参与 JSON：与字段 <see cref="questId" /> 在 CamelCase 下同名冲突。</summary>
    [JsonIgnore]
    public string QuestId => questId;

    [JsonIgnore]
    public int? AcceptedGameTime
    {
        get => acceptedMinutes < 0 ? null : acceptedMinutes;
        private set => acceptedMinutes = value.HasValue ? value.Value : -1;
    }

    [JsonIgnore]
    public int? CompletedGameTime
    {
        get => completedMinutes < 0 ? null : completedMinutes;
        private set => completedMinutes = value.HasValue ? value.Value : -1;
    }

    /// <summary>不参与 JSON：与字段 <see cref="phases" /> 在 CamelCase 下同名冲突。</summary>
    [JsonIgnore]
    public IReadOnlyList<QuestPhase> Phases => phases;

    [JsonIgnore]
    public QuestPhase CurrentPhase =>
        phases != null && currentPhaseIndex >= 0 && currentPhaseIndex < phases.Count
            ? phases[currentPhaseIndex]
            : null;

    [JsonIgnore] public bool IsAccepted => AcceptedGameTime.HasValue;

    [JsonIgnore] public bool IsCompleted => CompletedGameTime.HasValue;

    public void NormalizeAfterDeserialize()
    {
        phases ??= new List<QuestPhase>();
        dependencies ??= new List<string>();
        foreach (var p in phases)
            p?.NormalizeAfterDeserialize();
        AttachGoalContexts();
    }

    public void AddPhase(QuestPhase phase)
    {
        if (phase == null) throw new ArgumentNullException(nameof(phase));
        phases ??= new List<QuestPhase>();
        phases.Add(phase);
    }

    private static bool PhaseGoalsAllComplete(QuestPhase phase)
    {
        return phase != null && phase.AllGoalsComplete;
    }

    private static PubSub ResolvePubSubBus()
    {
        return Context.HasInstance() ? Context.Instance.Messager : null;
    }

    public bool TryAccept()
    {
        if (IsAccepted && !IsCompleted)
            return false;
        if (IsCompleted && !repeatable)
            return false;

        if (QuestManager.HasInstance() && !QuestManager.Instance.AreDependenciesCompleted(this))
            return false;

        if (IsCompleted)
            ResetProgress();

        AcceptedGameTime = GetCurrentConvertedGameMinutes();
        CompletedGameTime = null;
        currentPhaseIndex = 0;

        AttachGoalContexts();

        if (autoBindPubSubGoalListeners)
            BindAllGoalPubSubListeners(ResolvePubSubBus());

        PublishQuestAccepted();
        return true;
    }

    public bool TryAdvancePhase()
    {
        if (!IsAccepted || IsCompleted)
            return false;

        var phase = CurrentPhase;
        if (phase == null || !PhaseGoalsAllComplete(phase))
            return false;

        if (phases == null || currentPhaseIndex >= phases.Count - 1)
            return true;

        currentPhaseIndex++;
        return false;
    }

    public bool TryComplete()
    {
        if (!IsAccepted || IsCompleted)
            return false;

        var phase = CurrentPhase;
        if (phase == null || !PhaseGoalsAllComplete(phase))
            return false;
        if (phases == null || currentPhaseIndex < phases.Count - 1)
            return false;

        var completedPhaseIndex = currentPhaseIndex;
        var completedPhaseId = phase?.phaseId ?? "";

        CompletedGameTime = GetCurrentConvertedGameMinutes();

        if (autoBindPubSubGoalListeners)
            UnbindAllGoalPubSubListeners(ResolvePubSubBus());

        PublishQuestPhaseComplete(completedPhaseIndex, completedPhaseId);
        PublishQuestComplete();
        return true;
    }

    private static int GetCurrentConvertedGameMinutes()
    {
        var round = 0;
        if (TurnManager.HasInstance())
            round = TurnManager.Instance.CurrentGameTime;
        return GameTimeConverter.TurnRoundToGameMinutes(round);
    }

    public bool TryAdvanceOrComplete()
    {
        if (!IsAccepted || IsCompleted) return false;

        var phase = CurrentPhase;
        if (phase == null || !PhaseGoalsAllComplete(phase)) return false;

        if (phases == null || currentPhaseIndex >= phases.Count - 1)
            return TryComplete();

        var previousIndex = currentPhaseIndex;
        var completedPhase = phases[previousIndex];
        currentPhaseIndex++;
        PublishQuestPhaseComplete(previousIndex, completedPhase?.phaseId ?? "");
        PublishPhaseAdvanced(previousIndex, currentPhaseIndex);
        return true;
    }

    private void PublishPhaseAdvanced(int previousPhaseIndex, int newPhaseIndex)
    {
        if (!Context.HasInstance()) return;
        if (phases == null) return;

        var prevPhase = previousPhaseIndex >= 0 && previousPhaseIndex < phases.Count
            ? phases[previousPhaseIndex]
            : null;
        var nextPhase = newPhaseIndex >= 0 && newPhaseIndex < phases.Count ? phases[newPhaseIndex] : null;

        var evt = new QuestPhaseAdvancedEvent
        {
            QuestId = questId,
            PreviousPhaseIndex = previousPhaseIndex,
            NewPhaseIndex = newPhaseIndex,
            PreviousPhaseId = prevPhase?.phaseId ?? "",
            NewPhaseId = nextPhase?.phaseId ?? ""
        };

        Context.Instance.Messager.Publish(evt).Forget();
    }

    private void PublishQuestAccepted()
    {
        if (!Context.HasInstance()) return;
        Context.Instance.Messager.Publish(new QuestAccepted {QuestId = questId}).Forget();
    }

    private void PublishQuestComplete()
    {
        if (!Context.HasInstance()) return;
        Context.Instance.Messager.Publish(new QuestComplete {QuestId = questId}).Forget();
    }

    void PublishQuestPhaseComplete(int phaseIndex, string phaseId)
    {
        if (!Context.HasInstance()) return;
        Context.Instance.Messager.Publish(new QuestPhaseComplete
        {
            QuestId = questId,
            PhaseIndex = phaseIndex,
            PhaseId = phaseId ?? ""
        }).Forget();
    }

    /// <summary>
    ///     当前阶段全部目标已完成时由 <see cref="QuestPhase" /> 触发，尝试推进阶段或完成整任务。
    /// </summary>
    internal void AdvanceWhenCurrentPhaseAllGoalsComplete()
    {
        TryAdvanceOrComplete();
    }

    private void AttachGoalContexts()
    {
        if (phases == null) return;
        foreach (var phase in phases)
        {
            if (phase?.goals == null) continue;
            foreach (var goal in phase.goals)
                goal?.AttachContext(this, phase);
        }
    }

    private void ResetProgress()
    {
        if (autoBindPubSubGoalListeners)
            UnbindAllGoalPubSubListeners(ResolvePubSubBus());

        CompletedGameTime = null;
        currentPhaseIndex = 0;
        if (phases == null) return;
        foreach (var p in phases)
        {
            if (p?.goals == null) continue;
            foreach (var g in p.goals)
                g?.Reset();
        }
    }

    private void BindAllGoalPubSubListeners(PubSub bus)
    {
        if (phases == null) return;
        foreach (var p in phases)
        {
            if (p?.goals == null) continue;
            foreach (var g in p.goals)
                g?.BindListening(bus, this);
        }
    }

    private void UnbindAllGoalPubSubListeners(PubSub bus)
    {
        if (phases == null) return;
        foreach (var p in phases)
        {
            if (p?.goals == null) continue;
            foreach (var g in p.goals)
                g?.UnbindListening(bus);
        }
    }

    /// <summary>读档后订阅句柄丢失时，对进行中任务先解绑再重绑。</summary>
    public void RebindPubSubGoalListeners()
    {
        if (!IsAccepted || IsCompleted) return;
        var bus = ResolvePubSubBus();
        UnbindAllGoalPubSubListeners(bus);
        BindAllGoalPubSubListeners(bus);
    }

    /// <summary>注销任务前可调用，避免残留 PubSub 订阅。</summary>
    public void ReleasePubSubGoalListeners()
    {
        UnbindAllGoalPubSubListeners(ResolvePubSubBus());
    }
}