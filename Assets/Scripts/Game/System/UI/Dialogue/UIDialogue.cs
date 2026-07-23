using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using RedBlueGames.Tools.TextTyper;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
public class UIDialogue : MonoBehaviour
{
    private Animator m_Animator;
    [SerializeField] private TextTyper m_DialogueText;
    [SerializeField] private List<Button> m_Options = new();
    [SerializeField] private List<TMP_Text> m_OptionValues = new();
    [SerializeField] private GameObject m_HeadIcon;

    private DialogueEntry m_CurrentEntry;
    private Dialogue _activeDialogue;
    private DialogueContext _dialogueContext;
    private UnityAction m_DialogueFinishCallback;
    private UniTaskCompletionSource m_DialogueCompleteTcs;

    /// <summary>每次开始新对话递增，用于忽略被覆盖的对话的结束动画事件。</summary>
    private int _dialogueRunId;

    /// <summary>当前正在等待结束动画 <see cref="AnimFinish"/> 的 run；无则 -1。</summary>
    private int _awaitingFinishAnimRunId = -1;

    private void Awake()
    {
        m_Animator = GetComponent<Animator>();
    }

    /// <summary>开始对话；结束时触发 <paramref name="callback"/>（与结束动画 <see cref="AnimFinish"/> 同步）。</summary>

    /// <summary>带 <see cref="DialogueContext"/> 的异步对话。</summary>
    public UniTask StartDialogueAsync(Dialogue dialogue, DialogueContext context,
        CancellationToken cancellationToken = default)
    {
        _dialogueContext = context;
        var utcs = new UniTaskCompletionSource();
        BeginDialogue(dialogue, null, utcs);
        if (cancellationToken.CanBeCanceled)
        {
            cancellationToken.Register(() =>
            {
                if (m_DialogueCompleteTcs != utcs) return;
                m_DialogueCompleteTcs.TrySetCanceled(cancellationToken);
                m_DialogueCompleteTcs = null;
            });
        }

        return utcs.Task;
    }

    private void BeginDialogue(Dialogue dialogue, UnityAction callback, UniTaskCompletionSource asyncTcs)
    {
        CancelPendingDialogueTask();

        _dialogueRunId++;
        _awaitingFinishAnimRunId = -1;

        m_DialogueCompleteTcs = asyncTcs;
        _activeDialogue = dialogue;
        m_CurrentEntry = dialogue.TreeRoot;
        m_DialogueFinishCallback = callback;
        m_Animator.Play("Anim_UIDialogue_StartDialogue");
        Debug.Log($"Begin Dialogue-{dialogue.name}");
        ShowDialogue();
    }

    private void CancelPendingDialogueTask()
    {
        if (m_DialogueCompleteTcs == null) return;
        m_DialogueCompleteTcs.TrySetCanceled(CancellationToken.None);
        m_DialogueCompleteTcs = null;
    }

    private void ProcessAcceptQuest(DialogueAcceptQuest entry)
    {
        var questId = entry != null ? entry.questId : null;
        if (string.IsNullOrEmpty(questId)) return;
        if (!QuestManager.HasInstance()) return;
        QuestManager.Instance.TryAcceptQuest(questId);
    }

    private void ShowDialogue()
    {
        if (m_CurrentEntry != null)
        {
            m_DialogueText.TypeText(m_CurrentEntry.Content);
        }
        else
        {
            DialogueFinish();
        }
    }

    public void AnimFinish()
    {
        if (_awaitingFinishAnimRunId < 0 || _awaitingFinishAnimRunId != _dialogueRunId)
            return;
        _awaitingFinishAnimRunId = -1;

        var finishedDialogue = _activeDialogue;
        _activeDialogue = null;
        if (finishedDialogue != null && !string.IsNullOrEmpty(finishedDialogue.uid) && Context.HasInstance())
            Context.Instance.Messager.Publish(new DialogueCompletedEvent { DialogueUid = finishedDialogue.uid }).Forget();

        m_DialogueFinishCallback?.Invoke();
        m_DialogueFinishCallback = null;
        m_DialogueCompleteTcs?.TrySetResult();
        m_DialogueCompleteTcs = null;
    }

    private void DialogueFinish()
    {
        m_DialogueText.TypeText(string.Empty);
        foreach (var opt in m_Options)
        {
            opt.gameObject.SetActive(false);
        }

        _awaitingFinishAnimRunId = _dialogueRunId;
        m_Animator.Play("Anim_UIDialogue_FinishDialogue");
    }

    public void OnBtnClick_Next()
    {
        if (m_DialogueText.IsSkippable() && m_DialogueText.IsTyping)
        {
            m_DialogueText.Skip();
            return;
        }

        if (m_CurrentEntry == null) return;

        switch (m_CurrentEntry)
        {
            case DialogueRoot start:
                m_CurrentEntry = start.First();
                ShowDialogue();
                break;
            case DialogueLines content:
                m_CurrentEntry = content.First();
                ShowDialogue();
                break;
            case DialogueAcceptQuest acceptQuest:
                ProcessAcceptQuest(acceptQuest);
                m_CurrentEntry = acceptQuest.First();
                ShowDialogue();
                break;
            case DialogueOption:
                ShowOptions();
                break;
        }
    }

    private void ShowOptions()
    {
        if (m_CurrentEntry == null || m_CurrentEntry is not DialogueOption option)
        {
            DialogueFinish();
            return;
        }

        var dialogueEntries = option.All();
        if (dialogueEntries.Count == 0)
        {
            DialogueFinish();
            return;
        }

        for (var i = 0; i < dialogueEntries.Count; i++)
        {
            var entry = dialogueEntries[i];

            if (m_Options.Count <= i) continue;
            m_Options[i].gameObject.SetActive(true);
            m_OptionValues[i].text = entry.Content;
        }
    }

    public void OnBtnClick_Option(int index)
    {
        if (m_CurrentEntry is not DialogueOption option)
            return;
        var options = option.All();
        if (index < 0 || index >= options.Count)
            return;

        var child = options[index];

        foreach (var opt in m_Options)
            opt.gameObject.SetActive(false);

        switch (child)
        {
            case DialogueOption childOption:
                m_CurrentEntry = childOption;
                ShowOptions();
                return;
            case DialogueLines content:
                m_CurrentEntry = content.First();
                break;
            case DialogueAcceptQuest aq:
                ProcessAcceptQuest(aq);
                m_CurrentEntry = aq.First();
                ShowDialogue();
                return;
            case DialogueRoot dr:
                m_CurrentEntry = dr.First();
                break;
            default:
                m_CurrentEntry = child;
                break;
        }

        ShowDialogue();
    }
}
