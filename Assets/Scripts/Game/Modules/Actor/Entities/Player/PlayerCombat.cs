using System;

/// <summary>玩家战斗模式：有敌对单位追击/警觉锁定玩家时进入；用于单格移动清路径、PubSub 通知（如后续 BGM）。</summary>
public partial class Player
{
    /// <summary>战斗模式变化（在玩家 <see cref="PubSubActor.Messager"/> 上发布）。</summary>
    public sealed class CombatModeChangedEvent : EventArgs
    {
        public bool InCombat;
    }

    private bool _inCombatMode;

    public bool IsInCombatMode => _inCombatMode;

    /// <summary>根据当前场景内敌方 AI 状态刷新战斗模式并视变化发布 <see cref="CombatModeChangedEvent"/>。</summary>
    public static void RefreshCombatState(Player player)
    {
        if (player == null || !EntityManager.HasInstance()) return;

        var threat = false;
        foreach (var e in EntityManager.Instance.EnumerateAllEntities())
        {
            if (e is AIEntity enemy && enemy.IsThreateningPlayer(player))
            {
                threat = true;
                break;
            }
        }

        player.ApplyCombatModeThreat(threat);
    }

    private void ApplyCombatModeThreat(bool anyThreat)
    {
        if (_inCombatMode == anyThreat) return;
        _inCombatMode = anyThreat;
        _ = this.Publish(new CombatModeChangedEvent { InCombat = _inCombatMode });
    }
}
