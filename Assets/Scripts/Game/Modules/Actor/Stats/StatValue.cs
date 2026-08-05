using System;
using UnityEngine;

// ReSharper disable All

public class StatValue
{
    private float _baseValue;

    private bool m_IsDirty = true;
    private float m_Value;

    public float Value
    {
        get
        {
            if (!m_IsDirty) return m_Value;
            m_Value = CalculateFinalValue();
            m_IsDirty = false;
            return m_Value;
        }
    }

    public bool IsDirty()
    {
        return m_IsDirty;
    }
    
    private readonly StatModifierCollector[] m_ModifierCollector;
    public StatValue(float baseValue)
    {
        _baseValue = baseValue;

        var modifierTypes = (StatModifierType[])Enum.GetValues(typeof(StatModifierType));
        m_ModifierCollector = new StatModifierCollector[modifierTypes.Length];

        for (int i = 0; i < m_ModifierCollector.Length; i++)
        {
            m_ModifierCollector[i] = modifierTypes[i] switch
            {
                StatModifierType.AddFlat => new CollectorAdd(0.0f, StatModifierType.AddFlat),
                StatModifierType.AddPercent => new CollectorAdd(1.0f, StatModifierType.AddPercent),
                StatModifierType.MulPercent => new CollectorMul(1.0f, StatModifierType.MulPercent),
                StatModifierType.AddFinal => new CollectorAdd(0.0f, StatModifierType.AddFinal),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    private float CalculateFinalValue()
    {
        var finalValue = _baseValue;

        finalValue += m_ModifierCollector[(int) StatModifierType.AddFlat].FinalValue;
        finalValue *= m_ModifierCollector[(int) StatModifierType.AddPercent].FinalValue;
        finalValue *= m_ModifierCollector[(int) StatModifierType.MulPercent].FinalValue;
        finalValue += m_ModifierCollector[(int) StatModifierType.AddFinal].FinalValue;
     
        return (float) Math.Round(finalValue, 4);
    }
    
    public void UpdateBase(float baseValue)
    {
        if (_baseValue == baseValue)
            return;
        m_IsDirty = true;
        _baseValue = baseValue;
    }

    public void AddModifier(StatModifier modifier)
    {
        m_IsDirty = true;
        m_ModifierCollector[(int)modifier.ModType].AddModifier(modifier);
    }

    public bool RemoveModifier(StatModifier modifier)
    {
        if (m_ModifierCollector[(int)modifier.ModType].RemoveModifier(modifier))
        {
            m_IsDirty = true;
            return true;
        }
        return false;
    }

    public virtual bool RemoveAllModifiersFromSource(object source)
    {
        var didRemove = false;
        foreach (var collector in m_ModifierCollector)
        {
            if (collector.RemoveAllModifiersFromSource(source))
            {
                didRemove = true;
            }
        }

        if (didRemove)
        {
            m_IsDirty = true;
        }

        return didRemove;
    }
    
}