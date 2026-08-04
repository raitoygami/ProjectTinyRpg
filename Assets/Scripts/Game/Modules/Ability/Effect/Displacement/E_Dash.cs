using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

[AbilityEffectMenu("Displacement/Dash")]
public class E_Dash : AbilityEffect
{
    [Tooltip("为 false 时受单位与障碍层阻挡；为 true 时无视这些层，只受地形可行走限制，沿直线落到选中终点前最后一个仍可站立的格子")]
    [SerializeField] private bool ignoreBlocking;
    [Min(1.0f)]
    [SerializeField] private float Velocity = 1.0f;

    [SerializeField] private Ease _easeMod = Ease.Linear;
    protected override async UniTask OnApply()
    {
        var mover = m_Context.Owner.GetComponent<AgentMover>();
        var animator = m_Context.Owner.GetComponent<AgentAnimations>();
        var origin = m_Context.Owner.GridPosition;
        var target = m_Context.Position;
        var line = origin.LineTo(target);

        var pulledGrid = line[0];

        for (var i = 1; i < line.Count; i++)
        {
            var grid = line[i];

            if (PathFinder.Instance.CanPlaceFootprint(m_Context.Owner, (int)grid.x, (int)grid.z, (int)target.x, (int)target.z))
            {
                pulledGrid = grid;
            }
            else
            {
                break;
            }
        }

        if (pulledGrid.Dist(origin) > 0)
        {
            animator.FaceTarget(pulledGrid - origin);
            await mover.Move(pulledGrid, true, Velocity, _easeMod);
            await ApplyChildren();
        }
        else
        {
            m_Context.Cancel();
        }
    }
}
