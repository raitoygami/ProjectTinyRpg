using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AgentAbilities : MonoBehaviour
{
    public class AbilityUpdateEvt : EventArgs
    {
        public Ability Origin;
        public Ability Update;
    }

    private readonly AbilityUpdateEvt _AbilityUpdateEvt = new();
    private Ability WepAbility;

    public List<Ability> Abilities = new();
    
    public void UpdateWepAbility(Ability t_Ability)
    {
        _AbilityUpdateEvt.Origin = WepAbility;
        _AbilityUpdateEvt.Update = t_Ability;
        WepAbility = t_Ability;
        WepAbility.SetOnwer(gameObject.GetComponent<Entity>());
        this.Publish(_AbilityUpdateEvt);
    }

    public Ability GetWepAbility()
    {
        return WepAbility;
    }

    public bool GetTargets(Vector3 t_GridPosition, Ability t_Ability, ref List<Entity> t_Targets)
    {
        var cell = PathFinder.Instance.GetNode(t_GridPosition.x, t_GridPosition.y);
        var targetEntity = cell?.Logical as Entity;
        if (targetEntity == null)
            return false;

        var fraction = GetComponent<Entity>().Faction;
        t_Targets = new List<Entity>();
 
        var entity = targetEntity;
        switch (t_Ability.TargetMode())
        {
            case Ability.AbilityTargetMode.None:
                break;
            case Ability.AbilityTargetMode.Self:
                if (entity == GetComponent<Entity>()) t_Targets.Add(entity);

                break;
            case Ability.AbilityTargetMode.Enemy:
                if (EntityManager.IsEnemyFraction(entity.Faction, fraction)) t_Targets.Add(entity);

                break;
            case Ability.AbilityTargetMode.Any:
                t_Targets.Add(entity);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return t_Targets.Count != 0;
    }


    // 仅用作移动攻击选取目标
    public List<Entity> GetTargetByMove(IPathNodeAgent mover, PathNode targetLocation)
    {
        var targets = new List<Entity>();
        if (mover == null || targetLocation == null)
            return targets;

        var dir = new Vector2Int(targetLocation.X - mover.X, targetLocation.Y - mover.Y);
        dir.x = Math.Clamp(dir.x, -1, 1);
        dir.y = Math.Clamp(dir.y, -1, 1);
        
        var myFraction = GetComponent<Entity>().Faction;
        for (var i = 0; i < 1; i++)
        {
            var anchorX = targetLocation.X + dir.x * i;
            var anchorZ = targetLocation.Y + dir.y * i;

            var sizeX = Mathf.Max(1, mover.GridSizeX);
            var sizeZ = Mathf.Max(1, mover.GridSizeZ);

            // 遍历 mover 放置在 targetLocation 后占据的所有格子
            for (var ox = 0; ox < sizeX; ox++)
            for (var oz = 0; oz < sizeZ; oz++)
            {
                var checkX = anchorX + ox;
                var checkZ = anchorZ + oz;

                var cell = PathFinder.Instance.GetNode(checkX, checkZ);
                if (cell?.Logical == null)
                    continue;

                // 排除自己（防止把自己算进去）
                if (ReferenceEquals(cell.Logical, mover))
                    continue;

                if (cell.Logical is not Entity e ||
                    !EntityManager.IsEnemyFraction(e.Faction, myFraction)) continue;
                
                if (targets.Contains(e)) continue;
                
                targets.Add(e);
                return targets;
            }
        }

        return targets;
    }

    public bool WithinBaseAttack(PathNode t_TargetLocation)
    {
        var baseAttack = GetWepAbility();
        if (baseAttack == null || t_TargetLocation == null)
            return false;

        if (baseAttack.isSkillOnCooldown())
            return false;
        
        var myNode = GetComponent<IPathNodeAgent>();
        if (myNode == null) return false;

        // 获取目标位置的 IPathNode（支持多尺寸）
        var targetCell = PathFinder.Instance.GetNode(t_TargetLocation.X, t_TargetLocation.Y);
        var targetNode = targetCell?.Logical;

        var targetSizeX = 1;
        var targetSizeZ = 1;

        if (targetNode != null)
        {
            targetSizeX = Mathf.Max(1, targetNode.GridSizeX);
            targetSizeZ = Mathf.Max(1, targetNode.GridSizeZ);
        }

        // 使用 Footprint 最小距离（支持双方多尺寸）
        var dist = PathFinder.FootprintManhattanDistance(
            myNode,
            t_TargetLocation.X,
            t_TargetLocation.Y,
            targetSizeX,
            targetSizeZ);

        return dist <= baseAttack.GetRange();
    }
}