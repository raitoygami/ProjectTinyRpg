using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public enum DamageType
{
    Physical, // 物理伤害 → 使用减法护甲

    Magical // 魔法伤害 → 使用百分比魔抗
    // 可扩展：True, Pure, ElementalFire, Poison 等，并在 DamageCalculator.RegisterReduction 注册
}

/// <summary>
///     在 <see cref="DamageCalculator.Resolve"/> 之前发布：可先经护盾等修正 <see cref="RawDamage"/>。
/// </summary>
public class PreDamageResolveEvent : EventArgs
{
    public AgentStats Attacker { get; set; }
    public AgentStats Defender { get; set; }
    public DamageType DamageType { get; set; }

    /// <summary>技能倍率后的基础伤害；护盾吸收后会减小，并传入 Resolve。</summary>
    public int RawDamage { get; set; }

    /// <summary>进入护盾结算前的数值（与首次 RawDamage 相同，便于 UI/统计）。</summary>
    public int DamageBeforeShield { get; set; }

    public int AbsorbedByShield { get; set; }
}

public partial class AgentStats
{
    /// <summary>
    ///     对目标造成指定类型伤害，由 DamageCalculator 统一结算物理/魔法与扩展类型。
    /// </summary>
    public async UniTask DealDamage(AgentStats target, DamageType type, int skillMultiplier = 100)
    {
        if (target == null) return;
        var baseDmg = DamageCalculator.GetBaseDamage(this, type, skillMultiplier);
        if (baseDmg <= 0) return;
//        DevLog.Log($"{name}攻击{target.name}.");

        var pre = new PreDamageResolveEvent
        {
            Attacker = this,
            Defender = target,
            DamageType = type,
            RawDamage = baseDmg,
            DamageBeforeShield = baseDmg,
            AbsorbedByShield = 0
        };

        await target.Publish(pre, sequential: true);

        if (pre.RawDamage <= 0)
            return;

        var result = DamageCalculator.Resolve(this, target, pre.RawDamage, type);
        result.AbsorbedByShield = pre.AbsorbedByShield;

        /*
        var typeStr = result.DamageType == DamageType.Physical ? "物理" : "魔法";
        var critStr = result.IsCrit ? " (暴击)" : "";
        var dodgeStr = result.IsDodged ? " (闪避)" : "";
        var absorbedByShieldStr = result.AbsorbedByShield > 0 ? $"护盾吸收{result.AbsorbedByShield}" : "";
        DevLog.Log(
            $"{target.name} {absorbedByShieldStr} 受到 {typeStr}伤害 {result.RawDamage} → 最终 {result.FinalDamage}{critStr}{dodgeStr}");
            */

        await target.ApplyHealthLoss(result);
        await UniTask.CompletedTask;
    }
}