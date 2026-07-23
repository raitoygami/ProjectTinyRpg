using System;
// ReSharper disable All

public class StatValue
{
    private float m_BaseValue;

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

    private readonly StatModifierCollector[] m_ModifierCollector;
    public StatValue(float t_BaseValue)
    {
        m_BaseValue = t_BaseValue;

        var modifierTypes = (StatModifierType[])Enum.GetValues(typeof(StatModifierType));
        m_ModifierCollector = new StatModifierCollector[modifierTypes.Length];

        for (int i = 0; i < m_ModifierCollector.Length; i++)
        {
            m_ModifierCollector[i] = modifierTypes[i] switch
            {
                StatModifierType.BaseAdd => new CollectorAdd(0.0f, StatModifierType.BaseAdd),
                StatModifierType.PercentAdd => new CollectorAdd(1.0f, StatModifierType.PercentAdd),
                StatModifierType.PercentMul => new CollectorMul(1.0f, StatModifierType.PercentMul),
                StatModifierType.TotalAdd => new CollectorAdd(0.0f, StatModifierType.TotalAdd),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    private float CalculateFinalValue()
    {
        var finalValue = m_BaseValue;

        finalValue += m_ModifierCollector[(int) StatModifierType.BaseAdd].FinalValue;
        finalValue *= m_ModifierCollector[(int) StatModifierType.PercentAdd].FinalValue;
        finalValue *= m_ModifierCollector[(int) StatModifierType.PercentMul].FinalValue;
        finalValue += m_ModifierCollector[(int) StatModifierType.TotalAdd].FinalValue;
     
        return (float) Math.Round(finalValue, 4);
    }
    
    public void UpdateBase(float t_BaseValue)
    {
        if (m_BaseValue == t_BaseValue)
            return;
        m_IsDirty = true;
        m_BaseValue = t_BaseValue;
    }

    public void AddModifier(StatModifier t_Modifier)
    {
        m_IsDirty = true;
        m_ModifierCollector[(int)t_Modifier.ModType].AddModifier(t_Modifier);
    }

    public bool RemoveModifier(StatModifier t_Modifier)
    {
        if (m_ModifierCollector[(int)t_Modifier.ModType].RemoveModifier(t_Modifier))
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