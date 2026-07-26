using System;
using Cysharp.Threading.Tasks;

/// <summary>玩家战斗模式：有敌对单位追击/警觉锁定玩家时进入；用于单格移动清路径、PubSub 通知（如后续 BGM）。</summary>
public partial class Player
{
    /// <summary>战斗模式变化（在玩家 <see cref="PubSubActor.Messager"/> 上发布）。</summary>
    public sealed class EnterCombatEvt : EventArgs
    {
    }

    private UniTask OnEnterCombatEvt(EnterCombatEvt arg)
    {
        ClearPath();
        if (TileSelector.HasInstance())
            TileSelector.Instance.ClearPath();
        return UniTask.CompletedTask;
    }
}
