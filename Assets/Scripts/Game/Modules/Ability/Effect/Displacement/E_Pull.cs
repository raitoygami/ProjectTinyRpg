using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

[AbilityEffectMenu("Displacement/Pull")]
public class E_Pull : AbilityEffect
{
    [Min(1.0f)]
    [SerializeField] private float Velocity = 1.0f;
    
    protected override async UniTask OnApply()
    {
        var mover = m_Context.Target.GetComponent<AgentMover>();
        if (mover == null)
            return;
        
        var origin = m_Context.Owner.GridPosition;
        var target = m_Context.Target.GridPosition;
        var line = origin.Line(target);
        line.Reverse();

        var pulledGrid = line.First();

        for (var i = 1; i < line.Count; i++)
        {
            var grid = line[i];
            if (mover.Moveable(grid))
                pulledGrid = grid;
            else
                break;
        }

        if (pulledGrid.Dist(origin) == 1)
        {
            await mover.Move(pulledGrid, true, Velocity);
            await ApplyChildren();
        }
        else
        {
            m_Context.Cancel();
        }
    }
}
