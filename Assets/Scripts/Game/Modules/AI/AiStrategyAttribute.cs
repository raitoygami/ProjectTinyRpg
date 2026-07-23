using System;
using cfg;

/// <summary>
/// 标记 <see cref="IAiStrategy"/> 实现类对应的 <see cref="AiPattern"/>，由 <see cref="AiStrategyRegistry"/> 反射注册类型，按实体创建实例。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AiStrategyAttribute : Attribute
{
    public AiPattern Pattern { get; }

    public AiStrategyAttribute(AiPattern pattern) => Pattern = pattern;
}
