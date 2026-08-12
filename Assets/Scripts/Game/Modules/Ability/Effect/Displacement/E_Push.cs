using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[AbilityEffectMenu("Displacement/Push")]
public class E_Push : AbilityEffect
{
    [SerializeField] private int Distance = 1;
    [Min(1)]
    [SerializeField] private float Velocity = 1.0f;
    protected override async UniTask OnApply()
    {
        var mover = m_Context.Target.GetComponent<AgentMover>();
        if (mover == null)return;
        var agentStat = m_Context.Target.GetComponent<AgentStats>();
        if (!agentStat.Targetable())
            return;
        
        var origin = m_Context.Owner.GridPosition;
        var target = m_Context.Target.GridPosition;
        
        var direction = (target - origin);
        
        var pushPosition = target;
        var line = target.Line(direction, Distance);

        foreach (var t in line)
        {
            var cell = PathFinder.Instance.GetCell(t.x, t.y);
            if (cell == null || !PathFinder.IsWalkableCellForce(cell, m_Context.Target))
                break;
            pushPosition = t;
        }
        
        if (target != pushPosition)
        {
            await UniTask.Delay(200);
            await mover.Move(pushPosition.Round(), true, Velocity);
            await UniTask.Delay(300);
            await ApplyChildren();        
        }
        
    }
}