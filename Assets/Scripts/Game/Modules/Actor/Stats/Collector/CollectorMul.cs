using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectorMul : StatModifierCollector
{
    public CollectorMul(float t_BaseValue, StatModifierType t_StatModifierType) : base(t_BaseValue, t_StatModifierType)
    {
    }

    protected override void AddOperation(StatModifier modifier, float t_BaseValue, float t_CurrentFinal, out float newValue)
    {
        newValue = t_CurrentFinal * (1 + modifier.Value);
    }
}
