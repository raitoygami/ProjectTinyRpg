using System;
using System.Collections.Generic;
using UnityEngine;

public static class AbilityUtil
{
    
    private static bool IsMoveableForMask(PathCell cell, IPathNodeAgent owner)
    {
        return cell != null && PathFinder.IsWalkableCell(cell, owner);
    }

    // 技能准备范围
    public static List<Vector3Int> GetAbilityPrepareRange(Ability ability, Entity owner, Vector3 castPoint)
    {
        if (owner == null && ability == null)
            return null;

        var castTargetingMode = ability.GetCastTargetingMode();
        // 指向性技能， 
        if (castTargetingMode == CastTargetingMode.Directed)
        {
            var direction = owner.GridPosition.Direction(castPoint);
            var ret = owner.GridPosition.Line(direction, ability.GetRange());
            return ret;
        }

        // 自动释放
        if (castTargetingMode == CastTargetingMode.Auto)
        {
            
        }
        
        return null;
    }
    
    public static List<Vector3Int> GetCastableRange(Ability ability, Entity owner)
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
