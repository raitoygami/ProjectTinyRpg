using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 在持续若干回合内，于宿主每次 <see cref="TurnActor.TurnStartedEvent"/> 时触发子 Effect。
/// 需配合 <see cref="AbilityEffect.DurationBased"/>（Inspector 会自动勾选）。
/// </summary>
[AbilityEffectMenu("Decorator/OverTime")]
public class E_OverTime : AbilityEffect
{
    [SerializeField] [Min(1)] private int turnCount = 3;

    private int _remaining;
    private Action _unsubscribe;

#if UNITY_EDITOR
    private void OnValidate()
    {
        DurationBased = true;
    }
#endif

    protected override async UniTask OnApply()
    {
        _remaining = Mathf.Max(1, turnCount);
        var host = ResolveHostEntity();
        if (host == null)
        {
            RemoveSelfFromCarrierStats();
            await UniTask.CompletedTask;
            return;
        }

        _unsubscribe = host.Subscribe<TurnActor.TurnStartedEvent>(OnTurnStarted);
        await UniTask.CompletedTask;
    }

    private Entity ResolveHostEntity()
    {
        return Target switch
        {
            EffectTarget.Target => m_Context.Target,
            EffectTarget.Self => m_Context.Owner,
            EffectTarget.None => m_Context.Owner,
            _ => m_Context.Owner
        };
    }

    private void RemoveSelfFromCarrierStats()
    {
        var stats = Target switch
        {
            EffectTarget.Target => m_Context.Target.GetComponent<AgentStats>(),
            _ => m_Context.Owner.GetComponent<AgentStats>(),
        };
        stats.RemoveEffect(this);
    }

    private async UniTask OnTurnStarted(TurnActor.TurnStartedEvent arg)
    {
        if (_remaining <= 0)
            return;

        await ApplyChildren();
        _remaining--;

        if (_remaining > 0)
            return;

        _unsubscribe?.Invoke();
        _unsubscribe = null;

        var stats = arg.Owner.GetComponent<AgentStats>();
        if (stats != null)
            stats.RemoveEffect(this);
    }

    public override void OnRemove()
    {
        _unsubscribe?.Invoke();
        _unsubscribe = null;
    }
}
