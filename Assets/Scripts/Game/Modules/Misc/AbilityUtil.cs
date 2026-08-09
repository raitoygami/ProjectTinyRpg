using System;
using System.Collections.Generic;
using UnityEngine;

public static class AbilityUtil
{
    
    private static bool IsMoveableForMask(PathCell cell, IPathNodeAgent owner)
    {
        return cell != null && PathFinder.IsWalkableCell(cell, owner);
    }

    // 目前只实现了直线方向型的技能(获取直线上的格子)
    // 后去会根据技能类型返回技能预览
    public static List<Vector3Int> CalculateRange(Ability ability, Entity owner, Vector3 targetPosition)
    {
        if (owner == null && ability == null)
            return null;
        var direction = owner.GridPosition.Direction(targetPosition);
        var ret = owner.GridPosition.Line(direction, ability.GetCastRange());
        return ret;
    }
    
    public static List<Vector3Int> CalculateRange(Ability ability, Entity owner)
    {
        var ret = new List<Vector3Int>();
        if (owner == null || ability == null)
            return ret;

        var sx = (int) owner.GridPosition.x;
        var sy = (int) owner.GridPosition.y;

        var nodes = new Dictionary<Vector3Int, PathCell>();
        if (nodes == null) throw new ArgumentNullException(nameof(nodes));
        
        var range = ability.GetCastRange();
        var xMin = sx - range;
        var xMax = sx + range;
        var yMin = sy - range;
        var yMax = sy + range;
        for (var x = xMin; x <= xMax; x++)
        for (var y = yMin; y <= yMax; y++)
        {
            var target = PathFinder.Instance.GetCell(x, y);
            if (!IsMoveableForMask(target, owner))
                continue;

            if (target.X != sx || target.Y != sy)
            {
                var path = PathFinder.Instance.Navigate(owner, sx, sy, target.X, target.Y);
                var navigate = path?.Count ?? 0;
                if (navigate > range || navigate == 0) continue;
            }

            var local = new Vector3Int(x, y, 0);
            nodes.Add(local, target);
            ret.Add(local);
        }

        return ret;
    }
    
    // 从预警中获取目标
    public static Entity GetCloseTarget(Entity owner, Ability ability, List<Vector3Int> range)
    {
        if (owner == null || ability == null || range == null || range.Count == 0)
            return null;
        Entity closeTarget = null;        
        var minDist = int.MaxValue;
        
        foreach (var cell in range)
        {
            var dist = (int) Mathf.Abs(cell.x - owner.GridPosition.x) + (int)Mathf.Abs(cell.y - owner.GridPosition.y);
            if (dist >= minDist) continue;
            var node = PathFinder.Instance.GetCell(cell.x, cell.y);
            if (node?.Logical == null)
                continue;
            var entity =  node.Logical as Entity;
            closeTarget = entity;
            minDist = dist;
        }

        return closeTarget;
    }
    
}
