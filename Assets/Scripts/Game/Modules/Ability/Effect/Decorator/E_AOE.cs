using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 按 <see cref="AbilityAffectRangeParam"/> 区域（圆 / 扇形 / 矩形）：
/// <see cref="requireTargetSelection"/> 为 true 时收集实体，对每名目标执行子效果；
/// 为 false 时对范围内每一格各执行一次子效果（<see cref="AbilityContext.Position"/> 为该格，格上单位可作为 <see cref="AbilityContext.Target"/>）；
/// 可配合 <see cref="requireEmptyGround"/> 仅对空地格执行。
/// </summary>
[AbilityEffectMenu("Decorator/AOE")]
public class E_AOE : AbilityEffect
{
    [Serializable]
    private enum AoeFactionFilter
    {
        /// <summary>对 Owner 而言为敌对（与 <see cref="AgentAbilities.GetAffectTarget"/> 一致）。</summary>
        Enemies,
        /// <summary>与 Owner 同 <see cref="EntityFaction"/>。</summary>
        Allies,
        /// <summary>范围内全部实体（仍受排除选项影响）。</summary>
        All,
    }

    [Tooltip("区域形状与尺寸（圆/扇形/矩形）；未配置则本效果不命中任何目标")]
    [SerializeReference]
    [SerializeField] private AbilityAffectRangeParam abilityAffectRangeParam;

    [SerializeField] private AoeFactionFilter factionFilter = AoeFactionFilter.Enemies;

    [Tooltip("为 true 时 Owner 若在范围内也作为目标之一")]
    [SerializeField] private bool includeOwner;

    [Tooltip("为 true 时忽略 Faction 为 Neutral 的实体")]
    [SerializeField] private bool excludeNeutral = true;

    [Tooltip("为 true：仅对区域内通过阵营筛选的实体各执行一次子效果（原逻辑）。为 false：对区域内每一格各执行一次子效果，不依赖是否点选实体。")]
    [SerializeField] private bool requireTargetSelection = true;

    [Tooltip("仅在「每格」模式（上一项为 false）下生效：为 true 时仅对格上无任何单位的空地执行子效果；为 false 则范围内每一格都执行。")]
    [SerializeField] private bool requireEmptyGround;

    public override string GetDescription()
    {
        var mode = requireTargetSelection ? "" : $" ·每格{(requireEmptyGround ? "·仅空地" : "")}";
        if (abilityAffectRangeParam is RangeCircleParam c)
            return $"AOE Circle r≤{c.radius} {factionFilter}{mode}";
        if (abilityAffectRangeParam is RangeSectorParam)
            return $"AOE Sector {factionFilter}{mode}";
        if (abilityAffectRangeParam is RangeRectParam)
            return $"AOE Rect {factionFilter}{mode}";
        if (abilityAffectRangeParam is RangePointParam)
            return $"AOE Point {factionFilter}{mode}";
        return abilityAffectRangeParam != null ? $"AOE {factionFilter}{mode}" : "AOE（未配置区域）";
    }

    protected override async UniTask OnApply()
    {
        var owner = m_Context.Owner;
        if (owner == null)
            return;

        
        if (!EntityManager.HasInstance())
            return;

        if (abilityAffectRangeParam == null)
            return;

        var ability = m_Context.Ability;
        if (ability == null)
            return;

        var ownerGrid = owner.GridPosition;
        var castGrid = m_Context.Position;
        if (!ability.TryGetSkillPreviewFrame(ownerGrid, castGrid, out var previewOrigin, out var skillFace))
            return;

        var shapeSet = AbilityAffectRangeParamPreview.EnumerateShapeCells(abilityAffectRangeParam, previewOrigin, skillFace);
        if (shapeSet == null || shapeSet.Count == 0)
            return;

        if (!requireTargetSelection)
        {
            var cellTasks = shapeSet.Select(grid =>
            {
                if (requireEmptyGround)
                {
                    var cellNode = PathFinder.Instance.GetCell(grid.x, grid.z);
                    if (cellNode?.Logical != null)
                        return UniTask.CompletedTask;
                }

                var ctx = m_Context;
                ctx.Position = grid;
                ctx.Target = ResolveTargetOnCell(owner, grid);
                return ApplyChildren(ctx);
            });
            await UniTask.WhenAll(cellTasks);
            return;
        }

        var matches = new List<Entity>();
        foreach (var entity in EntityManager.Instance.EnumerateAllEntities())
        {
            if (entity == null) continue;
            if (!includeOwner && entity == owner) continue;
            if (excludeNeutral && entity.Faction == EntityFaction.Neutral) continue;

            var grid = entity.GridPosition;
            if (!shapeSet.Contains(grid))
                continue;

            if (!PassesFilter(owner, entity)) continue;
            matches.Add(entity);
        }

        if (matches.Count == 0)
            return;

        var tasks = matches.Select(target =>
        {
            var ctx = m_Context;
            ctx.Target = target;
            return ApplyChildren(ctx);
        });
        await UniTask.WhenAll(tasks);
    }

    /// <summary>格上单位作为 Target；受 includeOwner / excludeNeutral / 阵营筛选，不通过则为 null（子效果仍对格子执行）。</summary>
    private Entity ResolveTargetOnCell(Entity owner, Vector3 grid)
    {
        var node = PathFinder.Instance.GetCell(grid.x, grid.z);
        var entity = node?.Logical as Entity;
        if (entity == null) return null;
        if (!includeOwner && entity == owner) return null;
        if (excludeNeutral && entity.Faction == EntityFaction.Neutral) return null;
        return PassesFilter(owner, entity) ? entity : null;
    }

    private bool PassesFilter(Entity owner, Entity entity)
    {
        return factionFilter switch
        {
            AoeFactionFilter.Enemies => EntityManager.IsEnemyFraction(entity.Faction, owner.Faction),
            AoeFactionFilter.Allies => entity.Faction == owner.Faction,
            AoeFactionFilter.All => true,
            _ => false
        };
    }
}
