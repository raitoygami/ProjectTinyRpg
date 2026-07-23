using UnityEngine;

/// <summary>
/// 单次伤害结算结果，用于飘字、日志与后续扩展。
/// </summary>
public struct DamageResult
{
    /// <summary>结算前的原始伤害（技能倍率等已应用）。</summary>
    public int RawDamage;

    /// <summary>扣除护甲/魔抗等后的最终伤害（已含暴击）。</summary>
    public int FinalDamage;

    /// <summary>是否被闪避（未造成伤害）。</summary>
    public bool IsDodged;

    /// <summary>是否暴击。</summary>
    public bool IsCrit;

    /// <summary>伤害类型。</summary>
    public DamageType DamageType;

    /// <summary>被护盾吸收的伤害（在 Resolve 之前从 Raw 中扣除的部分）。</summary>
    public int AbsorbedByShield;

    public Vector3 Direction;
    
    public static DamageResult Dodged(int rawDamage, DamageType type)
    {
        return new DamageResult
        {
            RawDamage = rawDamage,
            FinalDamage = 0,
            IsDodged = true,
            IsCrit = false,
            DamageType = type
        };
    }
}
