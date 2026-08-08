using System.Collections.Generic;
using UnityEngine;

public static class TargetSelector
{
    // 从预警中获取目标
    public static Entity GetCloseTarget(Entity owner, Ability ability, List<Vector3Int> telegraph)
    {
        if (owner == null || ability == null || telegraph == null || telegraph.Count == 0)
            return null;
        Entity closeTarget = null;        
        var minDist = int.MaxValue;
        
        foreach (var cell in telegraph)
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
