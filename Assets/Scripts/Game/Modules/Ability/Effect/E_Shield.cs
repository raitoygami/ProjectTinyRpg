using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

[AbilityEffectMenu("Defense/Shield")]
public class E_Shield : AbilityEffect
{
    [SerializeField] private int durationTurns = 3;
    [SerializeField] [Range(1, 500)] private int percentOfCasterMaxHealth = 10;

    private int _remainingTurns;
    private bool _removeScheduled;
    private Action _unsubPreDamage;
    private Action _unsubTurn;
    private AgentStats _shieldedStats;
    private Entity _shieldedEntity;

    /// <summary>当前剩余可吸收量（供 <see cref="AgentStats.TotalShieldAbsorption" /> 汇总）。</summary>
    public int AbsorptionRemaining { get; private set; }
#if UNITY_EDITOR
    private void Reset()
    {
        DurationBased = true;
    }
#endif
    internal void RefreshShield(int absorption, int turnsRemaining)
    {
        AbsorptionRemaining = absorption;
        _remainingTurns = turnsRemaining;
        _removeScheduled = false;
        _shieldedStats?.NotifyShieldChanged();
    }

    protected override async UniTask OnApply()
    {
        if (!DurationBased)
        {
            Debug.LogWarning(
                "E_Shield 必须勾选 Duration Based，否则会使用共享资源且护盾耗尽时无法安全移除。请启用该选项或使用 Reset 后的默认配置。");
            return;
        }

        _shieldedEntity = GetDurationEffectEntity(m_Context);
        _shieldedStats = _shieldedEntity != null ? _shieldedEntity.GetComponent<AgentStats>() : null;
        var caster = m_Context.Owner.GetComponent<AgentStats>();
        if (caster == null || _shieldedStats == null || _shieldedEntity == null)
            return;

        var amount = Mathf.Max(0, caster.MaxHealth * percentOfCasterMaxHealth / 100);
        if (amount <= 0)
            return;

        var existing = _shieldedStats.FindExistingShieldForRefresh(this);
        if (existing != null)
        {
            DevLog.Log("刷新护盾值", null, Color.chartreuse);
            existing.RefreshShield(amount, durationTurns);
            await ApplyChildren();
            _shieldedStats.RemoveEffect(this);
            return;
        }

        DevLog.Log($"{_shieldedEntity.name}获取{amount}护盾", this, Color.chartreuse);
        AbsorptionRemaining = amount;
        _remainingTurns = durationTurns;
        _unsubPreDamage = _shieldedEntity.Subscribe<PreDamageResolveEvent>(OnPreDamageResolve);
        _unsubTurn = _shieldedEntity.Subscribe<TurnActor.TurnStartedEvent>(OnTurnStarted);
        _shieldedStats.NotifyShieldChanged();

        await ApplyChildren();
    }

    private async UniTask OnPreDamageResolve(PreDamageResolveEvent e)
    {
        if (e == null || e.Defender != _shieldedStats || e.RawDamage <= 0 || AbsorptionRemaining <= 0)
        {
            await UniTask.CompletedTask;
            return;
        }

        var take = Mathf.Min(AbsorptionRemaining, e.RawDamage);
        AbsorptionRemaining -= take;
        e.RawDamage -= take;
        e.AbsorbedByShield += take;
        _shieldedStats.NotifyShieldChanged();
        if (AbsorptionRemaining <= 0)
            ScheduleRemoveAfterPublish();

        await UniTask.CompletedTask;
    }


    /// <summary>避免在 <see cref="PreDamageResolveEvent" /> 分发过程中同步 <see cref="RemoveEffect" /> 导致重入。</summary>
    private void ScheduleRemoveAfterPublish()

    {
        if (_removeScheduled) return;

        _removeScheduled = true;

        RemoveWhenIdle().Forget();
    }


    private async UniTask RemoveWhenIdle()

    {
        DevLog.Log($"{_shieldedEntity.name}被破盾", this, Color.red);

        await UniTask.Yield();

        if (_shieldedStats != null)

            _shieldedStats.RemoveEffect(this);
    }


    private async UniTask OnTurnStarted(TurnActor.TurnStartedEvent arg)

    {
        if (_shieldedEntity == null || arg.Owner != _shieldedEntity)

        {
            await UniTask.CompletedTask;

            return;
        }

        _remainingTurns--;
        if (_remainingTurns > 0)
        {
            await UniTask.CompletedTask;
            return;
        }

        _unsubPreDamage?.Invoke();
        _unsubPreDamage = null;
        _unsubTurn?.Invoke();
        _unsubTurn = null;

        DevLog.Log("护盾移除", this, Color.darkMagenta);

        if (_shieldedStats != null)
            _shieldedStats.RemoveEffect(this);

        await UniTask.CompletedTask;
    }

    public override void OnRemove()
    {
        _unsubPreDamage?.Invoke();
        _unsubPreDamage = null;
        _unsubTurn?.Invoke();
        _unsubTurn = null;
        _shieldedStats?.NotifyShieldChanged();
    }
}