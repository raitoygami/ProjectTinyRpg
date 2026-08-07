using System;
using System.Collections.Generic;
using System.Reflection;


public static class AiStrategyFactory
{
    private static readonly Dictionary<AIPattern, Type> StrategyTypes = new();

    static AiStrategyFactory()
    {
        foreach (var type in typeof(AiStrategyFactory).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(IAIStrategy).IsAssignableFrom(type))
                continue;
            var attr = type.GetCustomAttribute<AIStrategyAttribute>();
            if (attr == null)
                continue;
            StrategyTypes[attr.Pattern] = type;
        }
    }

    public static IAIStrategy Create(AIPattern pattern)
    {
        if (StrategyTypes.TryGetValue(pattern, out var t))
            return (IAIStrategy)Activator.CreateInstance(t);
        return null;
    }
}
