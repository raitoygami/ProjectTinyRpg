using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class E_VFX : AbilityEffect
{
    [SerializeField] private int Duration = 0;
    [SerializeField] private ParticleSystem Particle;
    [SerializeField] private Vector3 Offset;

    private int _Remaining;
    private GameObject _Instance;
    private Action DestroyAction;
    protected override async UniTask OnApply()
    {
        _Remaining = Duration;
        var parent = GetTargetPosition();
        var position = (parent != null ? parent.position : m_Context.Position) + Offset;
        _Instance = PoolManager.Instance.Get(Particle.gameObject, position, Quaternion.identity, parent); 
        _Instance.GetComponent<ParticleSystem>().Play();

        if (DurationBased)
        {
            var take = parent != null ? parent : m_Context.Owner.transform;
            _Instance.transform.SetParent(take);
            DestroyAction = m_Context.Target.GetComponent<Entity>().Subscribe<TurnActor.TurnStartedEvent>(OnTurnStart);
        }
        else
        {
            try
            {
                await UniTask.WaitUntil((() => !_Instance.GetComponent<ParticleSystem>().isPlaying));
                if (_Instance != null)
                    PoolManager.Instance.Return(_Instance, Particle.gameObject);
            }
            catch (Exception)
            {
                return;
            }
        }

        await ApplyChildren();
    }

    private async UniTask OnTurnStart(TurnActor.TurnStartedEvent arg)
    {
        _Remaining--;
        if (_Remaining <= 0)
        {
            DestroyAction?.Invoke();
            var agentStats = arg.Owner.GetComponent<AgentStats>();
            if (agentStats != null)
            {
                agentStats.RemoveEffect(this);
            }
        }

        await UniTask.CompletedTask;
    }

    public override void OnRemove()
    {
        if (_Instance != null)
            PoolManager.Instance.Return(_Instance, Particle.gameObject);
    }

    private Transform GetTargetPosition()
    {
        return Target switch
        {
            EffectTarget.None => null,
            EffectTarget.Self => m_Context.Owner.transform,
            EffectTarget.Target => m_Context.Target.transform,
            _ => null,
        };
    }
    
}
