using System;
using JetBrains.Annotations;


public readonly struct StatModifier : IEquatable<StatModifier>
{
    public readonly StatModifierType ModType;
    public readonly float Value;
    public readonly int Order;
    [CanBeNull] public readonly object Source;

    private StatModifier(float value, StatModifierType type, [CanBeNull] object source, int order)
    {
        Value = value;
        ModType = type;
        Order = order;
        Source = source;
    }
    
    public StatModifier(float value, StatModifierType type) : this(value, type, null, (int)type) { }

    public StatModifier(float value, StatModifierType type, object source) : this(value, type, source, (int)type) { }

    public bool Equals(StatModifier other)
    {
        return ModType == other.ModType && Value.Equals(other.Value) && Order == other.Order && Equals(Source, other.Source);
    }

    public override bool Equals(object obj)
    {
        return obj is StatModifier other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int) ModType, Value, Order, Source);
    }
}

