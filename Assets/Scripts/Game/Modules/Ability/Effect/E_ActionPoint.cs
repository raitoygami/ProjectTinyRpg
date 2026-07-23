using Cysharp.Threading.Tasks;
using UnityEngine;

[AbilityEffectMenu("Meta/ActionPoint")]
public class E_ActionPoint : AbilityEffect
{
    [SerializeField] private int delta = 1;

    protected override UniTask OnApply()
    {
        var entity = ResolveEntity();
        if (entity == null)
        {
            return UniTask.CompletedTask;
        }

        var turn = entity.GetComponent<TurnActor>();
        if (turn != null)
        {
            turn.AddActionPoints(delta);
        }

        return ApplyChildren();
    }

    private Entity ResolveEntity()
    {
        return Target switch
        {
            EffectTarget.Self => m_Context.Owner,
            EffectTarget.Target => m_Context.Target,
            EffectTarget.None => m_Context.Owner,
            _ => m_Context.Owner
        };
    }
}
