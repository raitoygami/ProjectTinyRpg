using System;
using System.Collections.Generic;
using System.Reflection;
using cfg;

/// <summary>
/// 通过 <see cref="AiStrategyAttribute"/> 自动注册策略类型，按 <see cref="AiPattern"/> 创建实例，未注册时回退到 <see cref="AiPattern.Default"/>。
/// </summary>
public static class AiStrategyRegistry
{
    private static readonly Dictionary<AiPattern, Type> StrategyTypes = new();

    static AiStrategyRegistry()
    {
        foreach (var type in typeof(AiStrategyRegistry).Assembly.GetTypes())
        {
            if (type.IsAbstract || !typeof(IAiStrategy).IsAssignableFrom(type))
                continue;
            var attr = type.GetCustomAttribute<AiStrategyAttribute>();
            if (attr == null)
                continue;
            StrategyTypes[attr.Pattern] = type;
        }

        if (!StrategyTypes.ContainsKey(AiPattern.Default))
            throw new InvalidOperationException(
                $"No {nameof(IAiStrategy)} registered for {nameof(AiPattern.Default)}. Add [AiStrategy(AiPattern.Default)] on a strategy class.");
    }

    public static IAiStrategy Create(AiPattern pattern)
    {
        if (!StrategyTypes.TryGetValue(pattern, out var t))
            t = StrategyTypes[AiPattern.Default];
        return (IAiStrategy)Activator.CreateInstance(t);
    }
}
