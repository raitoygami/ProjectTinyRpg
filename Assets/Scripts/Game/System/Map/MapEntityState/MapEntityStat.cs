using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class EntityStatData
{
        
}

[Serializable]
public class EntityStatDoor : EntityStatData
{
    public bool IsOpen;
}

[Serializable]
public class EnemyStatData
{
    public string UniqueID;
    public int EntityId;
    public Vector3 SpawnPosition;
    public Vector3 Location;
    public Vector3 Direction =  Vector3.one;
    public int HpLost;
    public bool IsAlive;
    
    
    public class EnemyAbilities
    {
        // 技能数据
        public Dictionary<int, AbilityStat> LookupTable = new();

        public AbilityStat GetAbilityStat(int abilityId)
        {
            LookupTable ??= new Dictionary<int, AbilityStat>();
            
            if (!LookupTable.TryGetValue(abilityId, out var abilityStat))
            {
                abilityStat = new AbilityStat(){Cooldown = 0, AbilityId = abilityId};
                LookupTable.Add(abilityId, abilityStat);
            }
            return abilityStat;
        }
    }
    
    public EnemyAbilities _abilities;

    public EnemyAbilities GetAbilities()
    {
        _abilities ??= new EnemyAbilities();
        return _abilities;
    }
    // 技能CD信息
    
    // 运行时状态
    // 比如战斗中的动态信息， buff , debuff, dot等等
    // 状态机信息
    
}
// 当玩家切换地图的时候，刷新？
[Serializable]
public class EntityStatEnemySpawner : EntityStatData
{
    public bool HasSpawned = false;
    public List<EnemyStatData> SpawnedEnemies = new();

    public void AddEnemyStatData(EnemyStatData statData)
    {
        SpawnedEnemies.Add(statData);
    }
}
