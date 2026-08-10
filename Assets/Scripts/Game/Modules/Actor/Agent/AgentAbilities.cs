using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AgentAbilities : MonoBehaviour
{
   
    private Ability _wepAtkAbilityActive;

    // 装备普通攻击技能,只存放装备在槽位上的武器对应技能
    private readonly Dictionary<int, Ability> _wepAtkAbilities = new();

    public async UniTask<Ability> GetWepAtkAbility(int abilityId)
    {
        if (!_wepAtkAbilities.TryGetValue(abilityId, out var ability))
        {
            var config = ConfigManager.Instance.GetAbility(abilityId);
            if (config == null) return null;
            
            var handle = Addressables.LoadAssetAsync<Ability>(config.Addressable);
            await handle.ToUniTask();
            ability = Instantiate(handle.Result);
            _wepAtkAbilities.Add(abilityId, ability);
        }
        
        return ability;
    }

    public async UniTask SyncWepAtkAbilityStat(int abilityId, AbilityStat stat)
    {
        var ability = await GetWepAtkAbility(abilityId);
        ability.SetAbilityStat(stat);
    }
    
    public async UniTask SyncWepAtkAbilityStat(Dictionary<int, AbilityStat> stats)
    {
        var owner = gameObject.GetComponent<Entity>();
        foreach (var (abilityId, ability) in _wepAtkAbilities)
        {
            if (!stats.TryGetValue(abilityId, out var abilityStat)) 
            {
                abilityStat = new AbilityStat(){AbilityId = abilityId, Cooldown = 0};
                stats.Add(abilityId, abilityStat);
            }
            ability.SetAbilityStat(abilityStat);
            ability.SetOwner(owner);
        }

        await UniTask.CompletedTask;
    }

    // 同步穿戴的武器技能
    // 没有装备的武器技能，直接卸载掉
    public async UniTask SyncWepAbilities(List<int> wepAtkAbilityIDs)
    {
        // 1. 找出需要移除的武器技能 ID（不在新列表中）
        var toRemove = _wepAtkAbilities.Keys
            .Where(id =>
            {
                var config = ConfigManager.Instance.GetAbility(id);
                return config is { AbilityType: AbilityType.WEAPON }
                       && !wepAtkAbilityIDs.Contains(id);
            })
            .ToList();
        
        // 2. 安全移除并销毁
        foreach (var id in toRemove)
        {
            if (!_wepAtkAbilities.Remove(id, out var ability)) continue;
            if (ability != null)
                Destroy(ability);
        }

        // 3. 添加新列表中的技能（GetAbility 会复用已存在的）
        foreach (var abilityId in wepAtkAbilityIDs)
        {
            await GetWepAtkAbility(abilityId);
        }
    }
    
    public async UniTask UpdateWepAbility(int abilityId)
    {
        var ability = await GetWepAtkAbility(abilityId);
        _wepAtkAbilityActive = ability;
        _wepAtkAbilityActive.SetOwner(gameObject.GetComponent<Entity>());
    }

    public Ability GetWepAbility()
    {
        return _wepAtkAbilityActive;
    }
    
    // get affect target
    public bool GetAffectTarget(Ability ability, Vector3Int castPoint, List<Vector3Int> castableRange, out List<Vector3Int> affectTargets)
    {
        var owner = GetComponent<Entity>();
        affectTargets = AbilityUtil.GetAbilityAffectRange(ability, owner, castableRange, castPoint);
        return affectTargets.Count > 0;
    }


    // 仅用作移动攻击选取目标
    public List<Vector3Int> GetTargetByMove(IPathNodeAgent mover, PathNode pathNode)
    {
        var targets = new List<Vector3Int>();
        
        if (mover == null || pathNode == null)
            return targets;
        
        var owner = GetComponent<Entity>();
        var ability = GetWepAbility();
        var castPoint = new Vector3(pathNode.Position.x, pathNode.Position.y, 0);
        var castableRange = AbilityUtil.GetCastableRange(ability, owner);
        var abilityAffectRange = AbilityUtil.GetAbilityAffectRange(ability, owner, castableRange, castPoint, true);

        var targetLocation = Vector3Int.FloorToInt(castPoint);
        if (abilityAffectRange.Contains(targetLocation))
        {
            targets.Add(targetLocation);
        }

        return targets;
 
        /*
        var sizeX = Mathf.Max(1, mover.GridSizeX);
        var sizeZ = Mathf.Max(1, mover.GridSizeZ);
        
        var dir = new Vector2Int(pathNode.X - mover.X, pathNode.Y - mover.Y);
        dir.x = Math.Clamp(dir.x, -1, 1);
        dir.y = Math.Clamp(dir.y, -1, 1);
        
        var myFraction = GetComponent<Entity>().Faction;
        for (var i = 0; i < 1; i++)
        {
            var anchorX = pathNode.X + dir.x * i;
            var anchorZ = pathNode.Y + dir.y * i;
            // 遍历 mover 放置在 targetLocation 后占据的所有格子
            for (var ox = 0; ox < sizeX; ox++)
            for (var oz = 0; oz < sizeZ; oz++)
            {
                var x = anchorX + ox;
                var y = anchorZ + oz;

                var cell = PathFinder.Instance.GetCell(x, y);
                if (cell?.Logical == null)
                    continue;

                // 排除自己（防止把自己算进去）
                if (ReferenceEquals(cell.Logical, mover))
                    continue;

                if (cell.Logical is not Entity e ||
                    !EntityManager.IsEnemyFraction(e.Faction, myFraction)) continue;

                var location = new Vector3Int(x, y, 0);
                
                if (targets.Contains(location)) continue;
                
                targets.Add(location);
            }
        }

        return targets;*/
    }

    public bool WithinBaseAttack(PathNode targetLocation)
    {
        var baseAttack = GetWepAbility();
        if (baseAttack == null || targetLocation == null)
            return false;

        if (baseAttack.IsOnCooldown())
            return false;
        
        var owner = GetComponent<Entity>();
        if (owner == null) return false;
        // 获取目标位置的 IPathNode（支持多尺寸）
        var cell = PathFinder.Instance.GetCell(targetLocation.X, targetLocation.Y);
        
        return AbilityUtil.IsTarget(baseAttack, owner, cell);
    }

    private void OnDestroy()
    {
        foreach (var (_, ability) in _wepAtkAbilities)
        {
            if (ability != null)
                Destroy(ability);
        }
        _wepAtkAbilities.Clear();
        
    }
}