public class CollectorAdd : StatModifierCollector
{
    public CollectorAdd(float t_BaseValue, StatModifierType t_StatModifierType) : base(t_BaseValue, t_StatModifierType)
    {
    }

    protected override void AddOperation(StatModifier modifier, float t_BaseValue, float t_CurrentFinal, out float newValue)
    {
        newValue = t_CurrentFinal + modifier.Value;
    }
}
