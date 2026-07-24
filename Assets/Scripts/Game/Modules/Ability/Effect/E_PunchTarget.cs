using UnityEngine;
using Cysharp.Threading.Tasks;

public class E_PunchTarget : AbilityEffect
{
    [SerializeField] private float Duration = 1;
    protected override async UniTask OnApply()
    {
        var agent = m_Context.Owner.GetComponent<AgentAnimations>();
        var gridDelta = m_Context.Target.GridPosition - m_Context.Owner.GridPosition;
        var worldDir = new Vector3(gridDelta.x, gridDelta.y, 0f);
        await agent.PunchTarget(worldDir, Duration);
        await ApplyChildren();
    }
}
