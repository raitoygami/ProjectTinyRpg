using Cysharp.Threading.Tasks;

/// <summary>
/// 将单位传送到指定格点：默认使用技能选中的 <see cref="AbilityContext.Position"/>，
/// 也可在资源上勾选使用固定的网格坐标。
/// </summary>
[AbilityEffectMenu("Displacement/Teleport")]
public class E_Teleport : AbilityEffect
{
    
    protected override async UniTask OnApply()
    {
        var entity = ResolveEntity();
        if (entity == null)
            return;

        var mover = entity.GetComponent<AgentMover>();
        if (mover == null)
            return;

        var dest = m_Context.Position;

        var node = PathFinder.Instance.GetNode((int)dest.x, (int)dest.y);
        if (node == null || !PathFinder.IsWalkableCell(node, m_Context.Owner))
        {
            m_Context.Cancel();
            return;
        }

        if (entity.GridPosition.Dist(dest) == 0)
        {
            await ApplyChildren();
            return;
        }

        await mover.Move(dest, true, 0);
        await ApplyChildren();
    }

    private Entity ResolveEntity()
    {
        return Target switch
        {
            EffectTarget.Target => m_Context.Target,
            _ => m_Context.Owner,
        };
    }
}
