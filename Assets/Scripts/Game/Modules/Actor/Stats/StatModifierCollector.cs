using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public abstract class StatModifierCollector
{
    protected StatModifierType m_ModifierType;
    protected float m_BaseValue;
    protected float m_CurrentFinal;
    protected bool m_IsDirty;
    protected readonly List<StatModifier> m_StatModifiers;

    public float FinalValue => CalculateFinalValue();

    private readonly PredicateClosure m_PredicateClosure;

    private class PredicateClosure
    {
        public readonly Predicate<StatModifier> Predicate;
        [CanBeNull] public object SourceToRemove;

        public PredicateClosure()
        {
            Predicate = modifier => modifier.Source == SourceToRemove;
        }
    }
    protected StatModifierCollector(float t_BaseValue, StatModifierType t_StatModifierType)
    {
        m_IsDirty = false;
        m_BaseValue = t_BaseValue;
        m_CurrentFinal = t_BaseValue;
        m_StatModifiers = new List<StatModifier>();
        m_PredicateClosure = new PredicateClosure();
    }
    
    public void AddModifier(StatModifier t_Modifier)
    {
        m_StatModifiers.Add(t_Modifier);
        m_IsDirty = true;
    }

    public bool RemoveModifier(StatModifier t_Modifier)
    {
        if (m_StatModifiers.Remove(t_Modifier))
        {
            m_IsDirty = true;
            return true;
        }
        return false;
    }
    
    public virtual bool RemoveAllModifiersFromSource(object source)
    {
        m_PredicateClosure.SourceToRemove = source;
        var numRemovels = m_StatModifiers.RemoveAll(m_PredicateClosure.Predicate);
        m_PredicateClosure.SourceToRemove = null;

        var didRemove = false;
        if (numRemovels > 0)
        {
            m_IsDirty = didRemove = true;
        }

        return didRemove;
    }

    private float CalculateFinalValue()
    {
        if (!m_IsDirty) return m_CurrentFinal;

        m_CurrentFinal = m_BaseValue;
        foreach (var modifier in m_StatModifiers)
        {
            AddOperation(modifier, m_BaseValue, m_CurrentFinal, out m_CurrentFinal);
        }
        
        m_IsDirty = false;
        return m_CurrentFinal;
    }
    
    protected abstract void AddOperation(StatModifier modifier, float t_BaseValue, float t_CurrentFinal,
        out float newValue);
    
}
