using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     独立伤害计算器：负责基础伤害、物理/魔法减伤、闪避与暴击结算，便于扩展新伤害类型。
/// </summary>
public static class DamageCalculator
{
    private static readonly Dictionary<DamageType, IDamageReduction> ReductionHandlers = new();

    static DamageCalculator()
    {
        RegisterReduction(DamageType.Physical, new PhysicalDamageReduction());
        RegisterReduction(DamageType.Magical, new MagicalDamageReduction());
    }

    /// <summary>
    ///     注册自定义伤害类型的减伤逻辑，便于后续扩展（如 TrueDamage、Poison 等）。
    /// </summary>
    private static void RegisterReduction(DamageType type, IDamageReduction reduction)
    {
        if (reduction == null) return;
        ReductionHandlers[type] = reduction;
    }

    /// <summary>
    ///     根据攻击方属性与伤害类型计算基础伤害（含技能倍率）。
    /// </summary>
    /// <param name="attacker">攻击方</param>
    /// <param name="type">伤害类型</param>
    /// <param name="skillMultiplier">技能倍率，100 表示 100%</param>
    public static int GetBaseDamage(AgentStats attacker, DamageType type, int skillMultiplier = 100)
    {
        if (attacker == null) return 0;
        var baseVal = type switch
        {
            DamageType.Physical => attacker.PhysicalAttack,
            DamageType.Magical => attacker.MagicalAttack,
            _ => 0
        };
        return baseVal * Mathf.Max(0, skillMultiplier) / 100;
    }

    /// <summary>
    ///     完整结算一次伤害：减伤、闪避、暴击，不修改 defender 血量；由调用方根据 result 调用 defender.ApplyHealthLoss(result)。
    /// </summary>
    /// <param name="attacker">攻击方</param>
    /// <param name="defender">防守方</param>
    /// <param name="rawDamage">原始伤害（通常来自 GetBaseDamage）</param>
    /// <param name="type">伤害类型</param>
    /// <returns>结算结果，含最终伤害、是否闪避、是否暴击</returns>
    public static DamageResult Resolve(AgentStats attacker, AgentStats defender, int rawDamage, DamageType type)
    {
        if (defender == null || rawDamage <= 0)
            return new DamageResult { RawDamage = rawDamage, FinalDamage = 0, DamageType = type };

        // 1. 闪避（当前仅物理可闪避，可扩展配置）
        if (type == DamageType.Physical && defender.DodgeChance > 0)
            if (Random.Range(0, 100) < defender.DodgeChance)
                return DamageResult.Dodged(rawDamage, type);

        // 2. 减伤
        var afterReduction = ReductionHandlers.TryGetValue(type, out var reduction)
            ? reduction.Reduce(attacker, defender, rawDamage)
            : rawDamage;
        afterReduction = Mathf.Max(1, afterReduction);

        // 3. 暴击（当前使用攻击方暴击率/倍率）
        var isCrit = false;
        if (attacker.CritChance > 0 && Random.Range(0, 100) < attacker.CritChance)
        {
            afterReduction = afterReduction * attacker.CritMultiplier / 100;
            isCrit = true;
        }

        var finalDamage = Mathf.Max(1, afterReduction);
        var direction = defender.transform.SnapToGrid() - attacker.transform.SnapToGrid();
        return new DamageResult
        {
            RawDamage = rawDamage,
            FinalDamage = finalDamage,
            IsDodged = false,
            IsCrit = isCrit,
            DamageType = type,
            Direction = direction
        };
    }
}

/// <summary>
///     物理伤害：减法护甲，穿甲生效，至少 1 点伤害。
/// </summary>
public class PhysicalDamageReduction : IDamageReduction
{
    public int Reduce(AgentStats attacker, AgentStats defender, int rawDamage)
    {
        if (defender == null) return rawDamage;
        var effectiveArmor = Mathf.Max(0, defender.Armor - (attacker?.ArmorPenetration ?? 0));
        return Mathf.Max(rawDamage - effectiveArmor, 1);
    }
}

/// <summary>
///     魔法伤害：百分比魔抗，魔穿生效。公式：damage * 100 / (100 + effectiveMR)。
/// </summary>
public class MagicalDamageReduction : IDamageReduction
{
    public int Reduce(AgentStats attacker, AgentStats defender, int rawDamage)
    {
        if (defender == null) return rawDamage;
        var effectiveMR = Mathf.Max(0, defender.MagicResist - (attacker?.MagicPenetration ?? 0));
        return rawDamage * 100 / (100 + effectiveMR);
    }
}