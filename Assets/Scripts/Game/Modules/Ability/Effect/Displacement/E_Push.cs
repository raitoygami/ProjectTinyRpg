using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[AbilityEffectMenu("Displacement/Push")]
public class E_Push : AbilityEffect
{
    [SerializeField] private int Distance = 1;
    protected override async UniTask OnApply()
    {
        var mover = m_Context.Target.GetComponent<AgentMover>();
        if (mover == null)return;
        
        var origin = m_Context.Owner.GridPosition;
        var target = m_Context.Target.GridPosition;
        
        var direction = (target - origin).normalized;
        
        var pushPosition = target;
        for (var i = 1; i <= Distance; i++)
        {
            var t = pushPosition + direction;
            var gridNode = t.Round();
            var cell = PathFinder.Instance.GetNode(gridNode.x, gridNode.y);

            if (cell == null || !PathFinder.IsWalkableCell(cell, m_Context.Target))
                break;
            
            pushPosition = t;
        }
        
        if (target != pushPosition)
        {
            await UniTask.Delay(200);
            await mover.Move(pushPosition.Round(), true, 0.1f);
            await UniTask.Delay(300);
            await ApplyChildren();        
        }
        
    }
}