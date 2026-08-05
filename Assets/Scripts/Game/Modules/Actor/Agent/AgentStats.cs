using System;
using System.Collections.Generic;
using System.Linq;
using cfg.Defination;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Attribute = cfg.Defination.Attribute;
using t_Entity = cfg.t_Entity;

public partial class AgentStats : MonoBehaviour
{
    private t_Entity _entityConfig;

    /// <summary>当前单位对应的实体表配置（怪物/角色模板），便于查 drop_id、addressable 等。</summary>
    public t_Entity EntityConfig => _entityConfig;

    public void SetEntityConfig(t_Entity entity) => _entityConfig = entity;
    /// <summary>
    /// 角色 HP 归零被击败时发布，供 Player/<see cref="AIEntity"/> 等订阅处理。
    /// </summary>
    public class DefeatedEvent : EventArgs
    {
        public AgentStats DefeatedStats { get; set; }
        public DamageResult? LastDamage { get; set; }
    }

    /// <summary>
    /// HP 变化时发布（PubSub），供 StatBar 等 UI 同步显示。
    /// </summary>
    public class HealthChangedEvent : EventArgs
    {
        public AgentStats Stats { get; set; }
        public int Current { get; set; }
        public int Max { get; set; }

        public int HpChanged;
        public int HpLost;

    }

    public class TakeDamageEvent : EventArgs
    {
        public int Damage { get; set; }
        public int HpMax { get; set; }

        public Vector3 Direction;
    }
    
    /// <summary>护盾剩余总量变化时发布（所有 <see cref="E_Shield"/> 吸收量之和）。</summary>
    public class ShieldChangedEvent : EventArgs
    {
        public AgentStats Stats { get; set; }
        public int Current { get; set; }
    }

    public int Level { get; private set; }

    private StatValue _mana;
    private StatValue _health;

    // ---------- 基于 cfg.Defination.Attribute 的属性（StatValue），与 Attribute 字段一一对应 ----------
    private StatValue _strength;
    private StatValue _dexterity;
    private StatValue _intelligence;
    private StatValue _vitality;
    private StatValue _lucky;

    // 基础攻击 （角色模板+装备提供）
    private StatValue _baseAttack;
    private StatValue _critChance;
    private StatValue _critMultiplier;
    private StatValue _armor;
    private StatValue _magicResist;

    [Header("=== 穿透属性（攻击者提供，可通过装备/技能加成） ===")]
    private StatValue _armorPenetration; // 减防（穿甲）

    private StatValue _magicPenetration; // 减抗（魔穿）

    // 闪避
    private StatValue _evade;

    // Strength
    // STR: (Strength)HP + 1 | PhysicalMulti 5% | Armor 0.2
    public int STR => (int)(_strength?.Value ?? 0f);

    // Dexterity（敏捷）：影响暴击与闪避（输出/敏捷向）
    // DEX: CritChance+%1 | CritMultiplier + 2% | Dodge + 0.2%
    public int DEX => (int)(_dexterity?.Value ?? 0f);

    // Intelligence（智力）：影响法力与魔法抗性（法师向）
    // INT: Mana + 2 | MagicalAttack + 4 | MagicResist + 2
    public int INT => (int)(_intelligence?.Value ?? 0f);

    // Vitality（体力）：影响生命与魔法抗性（生存向）
    // VIT: Hp + 2 | Mana + 1 | Armor + （每5点+1） | MagicResist + (每5点+1)
    public int VIT => (int)(_vitality?.Value ?? 0f);

    public int BaseAttack => (int)(_baseAttack?.Value ?? 0);

    // 派生属性
    public int MaxHealth => (int)_health.Value + VIT * 2 + STR;
    public int MaxMana => (int)(_mana.Value + VIT * 1 + INT * 2);

    public int PhysicalMulti => 75 + STR * 5;
    // 派生属性 - 攻击力（全部 int）
    public int PhysicalAttack => (int)(BaseAttack * (PhysicalMulti / 100.0f));

    public int MagicalAttack => INT * 4;

    // VIT × 2 + (STR // 2)
    public int ArmorResist => (int)_armor.Value + VIT / 5 + STR / 5;

    // INT × 2 + (VIT // 3)
    public int MagicResist => (int)_magicResist.Value + INT * 2 + VIT / 5;

    // 穿透属性（攻击者提供，可通过装备/技能加成)
    public int ArmorPenetration => (int)_armorPenetration.Value; // 减防（穿甲）
    public int MagicPenetration => (int)_magicPenetration.Value; // 减抗（魔穿）

    public int CritChance => (int)(_critChance.Value + DEX);
    public int CritMultiplier => (int)(100 + _critMultiplier.Value + DEX * 2);
    public int Evade => (int)(_evade.Value + DEX / 5.0f);

    // stats
    private int _HealthLost;
    public int HealthCurrent => MaxHealth - _HealthLost;

    public void SetHealthLost(int value)
    {
        _HealthLost = Mathf.Clamp(value, 0, MaxHealth);
    }
    
    /// <summary>
    /// 应用伤害结果造成的血量扣除（由 DamageCalculator 结算后调用，便于飘字/事件扩展）。
    /// </summary>
    private async UniTask ApplyHealthLoss(DamageResult result)
    {
        if (result.FinalDamage <= 0) return;
        var lastHp = HealthCurrent;
        _HealthLost = Mathf.Clamp(_HealthLost + result.FinalDamage, 0, MaxHealth);
        var hpChanged = HealthCurrent - lastHp;
        _ = this.Publish(new HealthChangedEvent
        {
            Stats = this, 
            Current = HealthCurrent, 
            Max = MaxHealth,
            HpChanged = hpChanged,
            HpLost = _HealthLost,
        });
        await this.Publish(new TakeDamageEvent { Damage = result.FinalDamage, HpMax = MaxHealth , Direction = result.Direction});
        if (HealthCurrent <= 0)
        {
            await UniTask.Delay(250);
            var evt = new DefeatedEvent { DefeatedStats = this, LastDamage = result };
            await this.Publish(evt);
        }
        

        await UniTask.CompletedTask;
    }

    private void Awake()
    {
        _mana = new StatValue(0f);
        _health = new StatValue(0f);

        _baseAttack = new StatValue(0f);

        _strength = new StatValue(0f);
        _dexterity = new StatValue(0f);
        _intelligence = new StatValue(0f);
        _vitality = new StatValue(0f);

        _critChance = new StatValue(0f);
        _critMultiplier = new StatValue(0f);
        _armor = new StatValue(0f);
        _magicResist = new StatValue(0f);

        _armorPenetration = new StatValue(0);
        _magicPenetration = new StatValue(0);

        _evade = new StatValue(0);
    }

    /// <summary>
    ///     使用配置表 Attribute 设置各属性基础值（会 UpdateBase，不叠加）。
    /// </summary>
    public void SetBaseFromAttribute(Attribute attr)
    {
        if (attr == null) return;

        _health.UpdateBase(attr.Health);
        _mana.UpdateBase(attr.Mana);

        _baseAttack.UpdateBase(attr.BaseAttack);

        _strength.UpdateBase(attr.Strength);
        _dexterity.UpdateBase(attr.Dexterity);
        _intelligence.UpdateBase(attr.Intelligence);
        _vitality.UpdateBase(attr.Vitality);

        _critChance.UpdateBase(attr.CritChance);
        _critMultiplier.UpdateBase(attr.CritMultiplier);
        _armor.UpdateBase(attr.Armor);
        _magicResist.UpdateBase(attr.MagicResist);

        _evade.UpdateBase(attr.Dodge);

        /*if (GetComponent<Player>() != null)
            LogPlayerAttributesDebug();
        else
            DevLog.Log(
                $"{name}-的属性-MaxHealth:{MaxHealth}-PhysicalAttack:{PhysicalAttack}-Armor:{Armor}-MagicResist:{MagicResist}");*/
    }

    /// <summary>调试：打印玩家当前战斗属性（仅挂载 <see cref="Player"/> 时输出）。</summary>
    public void LogPlayerAttributesDebug()
    {
        DevLog.Log(
            $"{name} [Player属性] " +
            $"力量:{STR} 敏捷:{DEX} 智力:{INT} 体力:{VIT} | " +
            $"攻击:{BaseAttack} 物理攻击:{PhysicalAttack} 魔法攻击:{MagicalAttack} | " +
            $"血量:{HealthCurrent}/{MaxHealth} 蓝量:{MaxMana} | " +
            $"物抗:{ArmorResist} 法抗:{MagicResist} | " +
            $"物穿:{ArmorPenetration}-{ArmorPenetration} 法穿{MagicPenetration} | "  +
            $"暴击:{CritChance} 爆伤:{CritMultiplier} 闪避:{Evade}");
    }

    /// <summary>
    ///     根据 cfg.Defination.AttributeModifier（T=目标属性, M=加成类型, V=数值）添加一条修改器。
    /// </summary>
    /// <param name="modifier">属性修改器配置</param>
    /// <param name="source">修改器来源，用于后续 RemoveAttributeModifiersFromSource 移除</param>
    public void AddAttribute(AttributeModifier modifier, object source)
    {
        if (modifier == null) return;
        var stat = GetStatByAttributeType(modifier.T);
        if (stat == null) return;
        if (modifier.V == 0) return;
        var modType = ConfigModifierTypeToStat(modifier.M);
        stat.AddModifier(new StatModifier(modifier.V, modType, source));
    }



    private static StatModifierType ConfigModifierTypeToStat(AttributeModifierType m)
    {
        return (StatModifierType)(int)m;
    }

    private StatValue GetStatByAttributeType(AttributeType t)
    {
        return t switch
        {
            AttributeType.BaseAttack => _baseAttack,
            AttributeType.Strength => _strength,
            AttributeType.Dexterity => _dexterity,
            AttributeType.Intelligence => _intelligence,
            AttributeType.Vitality => _vitality,
            AttributeType.Health => _health,
            AttributeType.Mana => _mana,
            AttributeType.CritChance => _critChance,
            AttributeType.CritMultiplier => _critMultiplier,
            AttributeType.Armor => _armor,
            AttributeType.MagicResist => _magicResist,
            AttributeType.ArmorPenetration => _armorPenetration,
            AttributeType.MagicPenetration => _magicPenetration,
            AttributeType.Dodge => _evade,
            _ => null
        };
    }

    /// <summary>
    ///     批量添加多条 AttributeModifier（如装备/ buff 列表）。
    /// </summary>
    public void AddAttributeModifiersFromSource(IEnumerable<AttributeModifier> modifiers, object source)
    {
        if (modifiers == null) return;
        foreach (var m in modifiers)
            AddAttribute(m, source);
    }
    
    /// <summary>
    ///     移除来自指定来源（如某次 AddAttribute 的 source）的所有属性修正。
    /// </summary>
    public bool RemoveAttributeModifiersFromSource(object source)
    {
        var any = false;
        any |= _baseAttack.RemoveAllModifiersFromSource(source);
        any |= _strength.RemoveAllModifiersFromSource(source);
        any |= _dexterity.RemoveAllModifiersFromSource(source);
        any |= _intelligence.RemoveAllModifiersFromSource(source);
        any |= _vitality.RemoveAllModifiersFromSource(source);
        any |= _health.RemoveAllModifiersFromSource(source);
        any |= _mana.RemoveAllModifiersFromSource(source);
        any |= _critChance.RemoveAllModifiersFromSource(source);
        any |= _critMultiplier.RemoveAllModifiersFromSource(source);
        any |= _armor.RemoveAllModifiersFromSource(source);
        any |= _magicResist.RemoveAllModifiersFromSource(source);
        any |= _armorPenetration.RemoveAllModifiersFromSource(source);
        any |= _magicPenetration.RemoveAllModifiersFromSource(source);
        any |= _evade.RemoveAllModifiersFromSource(source);
        return any;
    }

    // ---------- 属性只读访问（与 cfg.Defination.Attribute 字段一致） ----------


    /// <summary>
    ///     获取对应 StatValue，用于外部添加/移除 Modifier。
    /// </summary>
    public StatValue GetStrengthStat()
    {
        return _strength;
    }

    public StatValue GetDexterityStat()
    {
        return _dexterity;
    }

    public StatValue GetIntelligenceStat()
    {
        return _intelligence;
    }

    public StatValue GetVitalityStat()
    {
        return _vitality;
    }

    public StatValue GetHealthStat()
    {
        return _health;
    }

    public StatValue GetManaStat()
    {
        return _mana;
    }

    public StatValue GetCritChanceStat()
    {
        return _critChance;
    }

    public StatValue GetCritMultiplierStat()
    {
        return _critMultiplier;
    }

    public StatValue GetArmorStat()
    {
        return _armor;
    }

    public StatValue GetMagicResistStat()
    {
        return _magicResist;
    }

    // ---------- AbilityEffect ----------

    private readonly List<AbilityEffect> m_AbilityEffect = new();

    public bool HasEffect<T>() where T : AbilityEffect
    {
        return m_AbilityEffect.OfType<T>().Any();
    }

    public void AddEffect(AbilityEffect t_Effect)
    {
        m_AbilityEffect.Add(t_Effect);
    }

    public void RemoveEffect(AbilityEffect effect)
    {
        if (!m_AbilityEffect.Remove(effect)) return;
        effect.OnRemove();
        Destroy(effect);
    }

    /// <summary>所有护盾剩余可吸收量之和。</summary>
    public int TotalShieldAbsorption
    {
        get
        {
            var sum = 0;
            foreach (var e in m_AbilityEffect)
            {
                if (e is E_Shield sh)
                    sum += sh.AbsorptionRemaining;
            }

            return sum;
        }
    }

    /// <summary>护盾总量变化时由 <see cref="E_Shield"/> 调用。</summary>
    public void NotifyShieldChanged()
    {
        _ = this.Publish(new ShieldChangedEvent { Stats = this, Current = TotalShieldAbsorption });
    }

    /// <summary>
    ///     查找除 <paramref name="incoming"/> 外、来自同一 <see cref="Ability"/> 的 <see cref="E_Shield"/>，用于重复施放时刷新而非叠加。
    /// </summary>
    public E_Shield FindExistingShieldForRefresh(E_Shield incoming)
    {
        var ability = incoming.GetContextAbility();
        foreach (var e in m_AbilityEffect)
        {
            if (e == incoming || e is not E_Shield sh) continue;
            if (sh.GetContextAbility() == ability)
                return sh;
        }

        return null;
    }

    public bool Targetable()
    {
        return HealthCurrent > 0;
    }
}