using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// 标记 <see cref="AbilityEffect"/> 在 Ability 图编辑器「创建节点」菜单中的路径（如 <c>Displacement/Pull</c>）。
/// 未标注的派生类默认使用类名并去掉 <c>E_</c> 前缀。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class AbilityEffectMenuAttribute : Attribute
{
    public string MenuPath { get; }

    public AbilityEffectMenuAttribute(string menuPath) => MenuPath = menuPath;
}

[Serializable]
public abstract partial class AbilityEffect : ScriptableObject
{
    [Serializable]
    public enum EffectTarget
    {
        None,
        Self,
        Target,
    }
    
    [SerializeField] public Ability Context;

    public int uid;
    public string Content;
    [SerializeField] public LocalizedString Description;
    
    [SerializeField] protected bool DurationBased = false;
    [SerializeField] protected EffectTarget Target = EffectTarget.None;
    [SerializeField] protected List<AbilityEffect> Children = new();

    protected AbilityContext m_Context;

    /// <summary>当前结算上下文所属技能（仅在一次 <see cref="Apply"/> 流程中有意义）。</summary>
    internal Ability GetContextAbility() => m_Context.Ability;

    public virtual string GetDescription()
    {
        /*if (Description != null)
        {
            return Description.GetLocalizedString();    
        }*/
        return string.Empty;
    }

    public void AddChild(AbilityEffect t_Child)
    {
        Children.Add(t_Child);
    }

    public void RemoveChild(AbilityEffect t_Child)
    {
        Children.Remove(t_Child);
    }
    
    public List<AbilityEffect> GetChildren()
    {
        return Children;
    }

    protected abstract UniTask OnApply();
    public virtual void OnRemove() {
    }
    
    public UniTask Apply(AbilityContext t_Context)
    {
        if (WrongTarget(t_Context))
        {
            return UniTask.CompletedTask;
        }
        
        var abilityEffect = this;
        if (DurationBased)
        {
            abilityEffect = Instantiate(this);
            TakeEffect(GetDurationEffectEntity(t_Context), abilityEffect);
        }

        abilityEffect.m_Context = t_Context;
        
        return abilityEffect.OnApply();
    }

    /// <summary>
    ///     Duration Based 时，效果实例挂载到哪个 <see cref="Entity"/> 的 <see cref="AgentStats"/>（与 <see cref="Target"/> 一致）。
    /// </summary>
    protected virtual Entity GetDurationEffectEntity(AbilityContext t_Context)
    {
        return Target switch
        {
            EffectTarget.None => t_Context.Owner,
            EffectTarget.Self => t_Context.Owner,
            EffectTarget.Target => t_Context.Target,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private bool WrongTarget(AbilityContext t_Context)
    {
        return Target switch
        {
            EffectTarget.None => false,
            EffectTarget.Self => false,
            EffectTarget.Target => t_Context.Owner == t_Context.Target || t_Context.Target == null,
            _ => false
        };
    }
    
    private void TakeEffect(Entity t_Target, AbilityEffect t_Effect)
    {
        var stats = t_Target.GetComponent<AgentStats>();
        if (stats != null)
        {
            stats.AddEffect(t_Effect);
        }
    }
    
    
    protected UniTask ApplyChildren()
    {
        return ApplyChildren(m_Context);
    }

    /// <summary>使用指定上下文对子效果逐个 <see cref="Apply"/>（彼此并行）。</summary>
    protected UniTask ApplyChildren(AbilityContext context)
    {
        return UniTask.WhenAll(from effect in Children select effect.Apply(context));
    }

    
}
