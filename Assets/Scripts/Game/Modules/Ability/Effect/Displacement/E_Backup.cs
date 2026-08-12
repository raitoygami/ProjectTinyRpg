using Cysharp.Threading.Tasks;
using UnityEngine;

[AbilityEffectMenu("Displacement/Backup")]
public class E_Backup : AbilityEffect
{
    [SerializeField] private int Distance = 1;
    [Min(1.0f)]
    [SerializeField] private int Velocity = 1;

    protected override async UniTask OnApply()
    {
        var mover = m_Context.Owner.GetComponent<AgentMover>();
        if (mover == null)return;
        
        var origin = m_Context.Owner.GridPosition;
        var target = m_Context.Target.GridPosition;
        
        var direction = (origin - target);
        
        var pushPosition = origin;

        var line = origin.Line(direction, Distance);

        foreach (var t in line)
        {
            var cell = PathFinder.Instance.GetCell(t.x, t.y);
            if (cell == null || !PathFinder.IsWalkableCellForce(cell, m_Context.Owner))
                break;
            pushPosition = t;
        }
        
        if (target != pushPosition)
        {
            await mover.Move(pushPosition.Round(), true, Velocity);
            await ApplyChildren();        
        }
    }
}
