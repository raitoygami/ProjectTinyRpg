using UnityEngine;
using Cysharp.Threading.Tasks;
[AbilityEffectMenu("Combat/SwordSlash")]
public class E_SwordSlash : AbilityEffect
{
    [SerializeField] private float Duration = 1;
    [SerializeField] private GameAudioSounds Sounds;
    protected override async UniTask OnApply()
    {
        var agent = m_Context.Owner.GetComponent<AgentAnimations>();
        var target = m_Context.Target != null ? m_Context.Target.GridPosition : m_Context.Position.SnapToGrid();
        var gridDelta = target - m_Context.Owner.GridPosition;
        var worldDir = new Vector3(gridDelta.x, gridDelta.y, 0f);
        
        Debug.Log($"Sword slash {worldDir}");
        
        await agent.SwordSlash(worldDir, Duration, Sounds);
        await ApplyChildren();
    }
}
