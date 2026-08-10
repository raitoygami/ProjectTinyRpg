using System;
using System.Collections.Generic;
using UnityEngine;

public static class AbilityUtil
{
    public static bool IsTarget(Ability ability, Entity owner, PathCell cell)
    {
        var logical = cell?.Logical;
        if (logical == null) return false;
        var entity = logical as Entity;
        if (entity == null) return false;

        switch (ability.TargetMode())
        {
            case AbilityTargetType.None:
                break;
            case AbilityTargetType.Self:
                return entity == owner;
            case AbilityTargetType.Enemy:
                return EntityManager.IsEnemyFraction(owner.Faction, entity.Faction);
            case AbilityTargetType.Any:
                return entity != null;
            case AbilityTargetType.EmptyGround:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return false;
    }
    
    
    private static bool IsMoveableForMask(PathCell cell, IPathNodeAgent owner)
    {
        return cell != null && PathFinder.IsWalkableCell(cell, owner);
    }

    public static List<Vector3Int> GetRealCastPosition(Ability ability, Entity owner, List<Vector3Int> affectTargets,
        Vector3 castPoint)
    {
        if (owner == null || ability == null || affectTargets == null)
            return null;
        var ret = new List<Vector3Int>();
        
        var targetMode = ability.TargetMode();
        switch (targetMode)
        {
            case AbilityTargetType.None:
            case AbilityTargetType.Self:
                ret.Add(Vector3Int.FloorToInt(owner.GridPosition));
                break;
            case AbilityTargetType.Enemy:
                GetEnemyTarget(ability, owner, affectTargets, castPoint, ref ret);
                break;
            case AbilityTargetType.Any:
            case AbilityTargetType.EmptyGround:
                ret.Add(Vector3Int.FloorToInt(castPoint));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return ret;
        
    }
    
    public static List<Vector3Int> GetAbilityTargetPositions(Ability ability, Entity owner, List<Vector3Int> affectTargets, Vector3 castPoint)
    {
        if (owner == null || ability == null)
            return null;

        var ret = new List<Vector3Int>();
        var targetMode = ability.TargetMode();
        switch (targetMode)
        {
            case AbilityTargetType.None:
            case AbilityTargetType.Self:
                break;
            case AbilityTargetType.Enemy:
                GetEnemyTarget(ability, owner, affectTargets, castPoint, ref ret);
                break;
            case AbilityTargetType.Any:
                break;
            case AbilityTargetType.EmptyGround:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return ret;
    }

    private static void GetEnemyTarget(Ability ability, Entity owner, List<Vector3Int> affectTargets, Vector3 castPoint, ref List<Vector3Int> ret)
    {
        var affectType = ability.GetAffectType();
        var castTargetingMode = ability.GetCastTargetingMode();
        if (castTargetingMode == CastTargetingMode.Directed)
        {
            // 找到直线上最近的敌人
            if (affectType == AffectType.SelectPoint)
            {
                var entity = GetCloseTarget(owner, ability, affectTargets, out var location);
                
                if (entity != null)
                {
                    ret.Add(location);
                    return;
                }
                
                if (affectTargets.Contains(Vector3Int.FloorToInt(castPoint)))
                    ret.Add(Vector3Int.FloorToInt(castPoint));
                foreach (var position in ret)
                {
                    Debug.Log(position);
                }
                Debug.Log(castPoint);
                return;
            }
        }
        
        Debug.Log($"{ability.name}-{affectType}-{castTargetingMode}");
    }
    
    // 技能准备范围
    public static List<Vector3Int> GetAbilityPrepareRange(Ability ability, Entity owner, Vector3 castPoint)
    {
        if (owner == null || ability == null)
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

    // 获取技能是实际效果
    public static List<Vector3Int> GetAbilityAffectRange(Ability ability
        , Entity owner
        , List<Vector3Int> prepareRange
        , Vector3 castPoint
        , bool ignoreAlly = false)
    {
        var affectRange = new List<Vector3Int>();
        
        var abilityAffectRangeParam = ability.GetAbilityAffectRangeParam();
        switch (abilityAffectRangeParam)
        {
            case RangePointParam _:
                var line= owner.GridPosition.Line(castPoint);
                foreach (var cell in line)
                {
                    if (!prepareRange.Contains(cell))
                        break;
                    
                    var node = PathFinder.Instance.GetCell(cell.x, cell.y);
                    if (node?.Logical == null)
                    {
                        // 阻挡 就返回
                        if (!IsMoveableForMask(node, owner))
                            break;
                        affectRange.Add(cell);
                        continue;
                    }
                    var entity = node.Logical as Entity;
                    // 不可能为null
                    if (entity == null)
                    {
                        if (!IsMoveableForMask(node, owner))
                            break;
                    }
                    else
                    {
                        // 先忽略自己，如果目标不是自己的话
                        if (entity == owner && ability.TargetMode() != AbilityTargetType.Self)
                            continue;
                        // 如果不忽略, 则只要是目标就必须干
                        if (!ignoreAlly)
                        {
                            affectRange.Add(cell);
                            break;
                        }
                        // 如果是盟友 就返回
                        if (EntityManager.IsEnemyFraction(owner.Faction, entity.Faction))
                        {
                            affectRange.Add(cell);
                            break;
                        }
                    }
                    
                    affectRange.Add(cell);
                }
                break;
            case RangeCircleParam _:
                break;
        }
        
        return affectRange;
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
    public static Entity GetCloseTarget(Entity owner, Ability ability, List<Vector3Int> range, out Vector3Int location)
    {
        location = Vector3Int.zero;
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
            location = cell;
        }

        return closeTarget;
    }
    
}
