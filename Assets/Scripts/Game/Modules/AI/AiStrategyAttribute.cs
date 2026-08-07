using System;
using cfg;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AIStrategyAttribute : Attribute
{
    public AIPattern Pattern { get; }

    public AIStrategyAttribute(AIPattern pattern) => Pattern = pattern;
}
