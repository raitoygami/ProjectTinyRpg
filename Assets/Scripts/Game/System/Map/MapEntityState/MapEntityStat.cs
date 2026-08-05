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
