using Cysharp.Threading.Tasks;
using UnityEngine;

[AbilityEffectMenu("Displacement/Backup")]
public class E_Backup : AbilityEffect
{
    [SerializeField] private int Distance = 1;
    protected override async UniTask OnApply()
    {
        var mover = m_Context.Owner.GetComponent<AgentMover>();
        if (mover == null)return;
        
        var origin = m_Context.Owner.GridPosition;
        var target = m_Context.Target.GridPosition;
        
        var direction = (target - origin).normalized;
        
        var pushPosition = origin;
        for (var i = 1; i <= Distance; i++)
        {
            var t = pushPosition - direction;
            var gridNode = t.Round();
            var cell = PathFinder.Instance.GetNode(gridNode.x, gridNode.y);

            if (cell == null || !PathFinder.IsWalkableCell(cell, m_Context.Owner))
                break;
            
            pushPosition = t;
        }
        
        if (target != pushPosition)
        {
            await mover.Move(pushPosition.Round(), true, 0.1f);
            await ApplyChildren();        
        }
    }
}
