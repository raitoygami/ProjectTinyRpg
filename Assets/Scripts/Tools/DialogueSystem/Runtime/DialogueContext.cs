using System;

/// <summary>
/// 运行时随对话传入：在 UI 完成接任务等操作后发布 <see cref="PublishPayload"/>。
/// 若设置了 <see cref="PublishBus"/> 则只向该 <see cref="PubSub"/> 发布；否则使用全局 <see cref="Context"/> 的 Messager。
/// </summary>
public sealed class DialogueContext
{
    /// <summary>左侧对话方（如 NPC / 立绘侧），可选。</summary>
    public UnityEngine.Object LeftPublisher { get; set; }

    /// <summary>右侧对话方（如玩家 / 对立侧），可选。</summary>
    public UnityEngine.Object RightPublisher { get; set; }

    /// <summary>
    /// 用于发布 <see cref="PublishPayload"/> 的独立总线；为 <c>null</c> 时回退到 <see cref="Context.Instance"/>.Messager（需 <see cref="Context"/> 已存在）。
    /// </summary>
    public PubSub PublishBus { get; set; }

    /// <summary>
    /// 在 <see cref="DialogueAcceptQuest"/> 节点确认（下一句）时发布；若为 <see cref="QuestAcceptedEvent"/>，运行时可由 <see cref="UIDialogue"/> 填入 <c>QuestId</c>。
    /// 为 <c>null</c> 时，接任务节点会发布新构造的 <see cref="QuestAcceptedEvent"/>。
    /// </summary>
    public EventArgs PublishPayload { get; set; }
}
