using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using cfg;
// ReSharper disable All

public class EntityManager : Singleton<EntityManager>
{
    private readonly Dictionary<EntityFaction, List<Entity>> _entities = new();

    /// <summary>遍历当前已注册的全部实体（按阵营分桶）。</summary>
    public IEnumerable<Entity> EnumerateAllEntities()
    {
        foreach (var list in _entities.Values)
        {
            if (list == null) continue;
            for (var i = 0; i < list.Count; i++)
                yield return list[i];
        }
    }
    
    /// <summary>Entity 注册时触发，用于 UI 等动态添加监听。</summary>
    public event Action<Entity> OnEntityRegistered;

    /// <summary>Entity 注销时触发，用于 UI 等移除监听与 StatBar。</summary>
    public event Action<Entity> OnEntityUnregistered;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;

    public void Init()
    {
        _entities.Clear();
    }

    public Dictionary<EntityFaction, List<Entity>> GetEntitiesTable()
    {
        return _entities;
    }
    
    /// <summary>设置 Player 预制体。</summary>
    public void SetPlayerPrefab(GameObject prefab) => playerPrefab = prefab;

    /// <summary>设置 <see cref="AIEntity"/> 预制体（关卡敌与技能召唤共用）。</summary>
    public void SetEnemyPrefab(GameObject prefab) => enemyPrefab = prefab;

    /// <summary>
    /// 在指定格子位置创建 Player，并用 t_Entity.Attr 初始化 AgentStats 属性。
    /// </summary>
    /// <param name="location">格子坐标（grid-based），Vector3 的 x、z 为格坐标。</param>
    /// <param name="entity">实体配置，为 null 则不初始化属性。</param>
    /// <returns>创建的 Player 组件，若 prefab 无效或未挂载 Player 则返回 null。</returns>
    public Player CreatePlayer(Vector3 location, t_Entity entity)
    {
        if (playerPrefab == null) return null;
        var l = location.GridToWorld();
        
        var go = Instantiate(playerPrefab, l, Quaternion.identity);
        var stats = go.GetComponent<AgentStats>();
        if (stats != null && entity?.Attr != null)
            stats.SetBaseFromAttribute(entity.Attr);
        stats?.SetEntityConfig(entity);
        
        return go.GetComponent<Player>();
    }

    /// <summary>
    /// 在指定格子位置创建 Player，使用配置表 entityId 解析 t_Entity 并初始化 AgentStats。需先 <see cref="ConfigManager.Init"/>。
    /// </summary>
    public Player CreatePlayer(Vector3 location, int entityId)
    {
        var entity = ConfigManager.Instance.Tables?.DataEntities.GetOrDefault(entityId);
        return CreatePlayer(location, entity);
    }

    /// <summary>
    /// 在指定格子位置创建敌对 <see cref="AIEntity"/>，并用 t_Entity 初始化属性与显示。
    /// </summary>
    /// <param name="location">格子坐标（grid-based），Vector3 的 x、z 为格坐标。</param>
    /// <param name="entity">实体配置，为 null 则不初始化属性与显示。</param>
    /// <returns>创建的组件，若 prefab 无效或未挂载 <see cref="AIEntity"/> 则返回 null。</returns>
    public AIEntity CreateEnemy(Vector3 location, t_Entity entity)
    {
        if (enemyPrefab == null) return null;
        var go = Instantiate(enemyPrefab, location.GridToWorld(), Quaternion.identity);
        var ai = go.GetComponent<AIEntity>();
        if (ai == null) return null;
        ai.ConfigureAsEnemy(entity);
        ai.GetComponent<IDynamicEntity>().InitAfterLevelLoad();
        return ai;
    }

    /// <summary>
    /// 在指定格子位置创建敌对 AI 实体，使用配置表 entityId 解析 t_Entity。需先 <see cref="ConfigManager.Init"/>。
    /// </summary>
    public AIEntity CreateEnemy(Vector3 location, int entityId)
    {
        var entity = ConfigManager.Instance.Tables?.DataEntities.GetOrDefault(entityId);
        return CreateEnemy(location.SnapToGrid(), entity);
    }

    /// 在指定格子生成召唤用 <see cref="AIEntity"/>（与关卡敌共用预制体），存在回合数耗尽后移除。
    public AIEntity CreateAIEntitySummon(Vector3 location, t_Entity entity, EntityFaction summonFaction,
        int lifetimeTurns, Entity summonOwner)
    {
        if (entity == null) return null;
        if (enemyPrefab == null) return null;
        var go = Instantiate(enemyPrefab, location.GridToWorld(), Quaternion.identity);
        var ai = go.GetComponent<AIEntity>();
        if (ai == null)
        {
            Destroy(go);
            return null;
        }

        ai.ConfigureAsSummon(summonFaction, entity, lifetimeTurns, summonOwner);
        return ai;
    }

    /// <summary>
    /// 使用配置表 entityId 解析 t_Entity 并生成召唤 <see cref="AIEntity"/>。需先 <see cref="ConfigManager.Init"/>。
    /// </summary>
    public AIEntity CreateAIEntitySummon(Vector3 location, int entityId, EntityFaction summonFaction,
        int lifetimeTurns, Entity summonOwner)
    {
        var entity = ConfigManager.Instance.Tables?.DataEntities.GetOrDefault(entityId);
        return CreateAIEntitySummon(location, entity, summonFaction, lifetimeTurns, summonOwner);
    }

    /// <summary>
    /// 根据施法者阵营解析召唤物阵营（玩家/玩家召唤物 → 玩家召唤物，敌方/敌方召唤物 → 敌方召唤物）。
    /// </summary>
    public static EntityFaction GetSummonFactionForOwner(EntityFaction ownerFaction)
    {
        return ownerFaction switch
        {
            EntityFaction.Player => EntityFaction.PlayerSummon,
            EntityFaction.PlayerSummon => EntityFaction.PlayerSummon,
            EntityFaction.Enemy => EntityFaction.EnemySummon,
            EntityFaction.EnemySummon => EntityFaction.EnemySummon,
            _ => EntityFaction.PlayerSummon
        };
    }

    public static void SpawnPlayer(Vector3 t_Location)
    {
    }

    public static void SpawnEnemy(Vector3 t_Location)
    {
        if (_instance == null) return;

        var enemy = new GameObject("AIEntity").AddComponent<AIEntity>();
        Register(enemy);
    }

    public static void Destroy(Entity t_Entity)
    {
        UnRegister(t_Entity);
        GameObject.Destroy(t_Entity.gameObject);
    }

    public static void DestroyAll()
    {
        if (_instance == null) return;
        foreach (var entity in _instance._entities.SelectMany(fractions => fractions.Value))
        {
            GameObject.Destroy(entity.gameObject);
        }

        _instance._entities.Clear();
    }

    // to by 
    public static void Register(Entity t_Entity)
    {
        if (_instance == null) return;
        _instance.RegisterInternal(t_Entity);
    }

    private void RegisterInternal(Entity t_Entity)
    {
        if (_entities.TryGetValue(t_Entity.Faction, out var fractions))
        {
            if (!fractions.Contains(t_Entity))
            {
                fractions.Add(t_Entity);
                OnEntityRegistered?.Invoke(t_Entity);
            }
            return;
        }

        var newFractions = new List<Entity> {t_Entity};
        _entities.Add(t_Entity.Faction, newFractions);
        OnEntityRegistered?.Invoke(t_Entity);
    }

    public static void UnRegister(Entity t_Entity)
    {
        if (!HasInstance()) return;
        _instance.UnRegisterInternal(t_Entity);
    }

    private void UnRegisterInternal(Entity t_Entity)
    {
        if (!_entities.TryGetValue(t_Entity.Faction, out var fractions)) return;
        if (fractions.Contains(t_Entity))
        {
            fractions.Remove(t_Entity);
            OnEntityUnregistered?.Invoke(t_Entity);
        }
    }

    public List<Entity> GetFractionEntities(EntityFaction t_Fraction)
    {
        return _entities.TryGetValue(t_Fraction, out var fractions) ? fractions : null;
    }

    private static readonly Dictionary<EntityFaction, EntityFaction[]> EnemyFactionsMap = new()
    {
        {EntityFaction.Enemy, new[] {EntityFaction.Player, EntityFaction.PlayerSummon}},
        {EntityFaction.Player, new[] {EntityFaction.Enemy, EntityFaction.EnemySummon}},
        {EntityFaction.PlayerSummon, new[] {EntityFaction.Enemy, EntityFaction.EnemySummon}},
        {EntityFaction.EnemySummon, new[] {EntityFaction.Player, EntityFaction.PlayerSummon}},
    };

    public static bool IsEnemyFraction(EntityFaction e1, EntityFaction e2)
    {
        return EnemyFactionsMap.TryGetValue(e1, out var entityFactions) && entityFactions.Contains(e2);
    }

    /// <summary>
    /// 每帧调用所有已注册实体的 <see cref="Entity.OnUpdate"/>
    /// </summary>
    private void Update()
    {
        if (_entities.Count == 0)
            return;

        foreach (var pair in _entities)
        {
            var list = pair.Value;
            if (list == null || list.Count == 0)
                continue;
            foreach (var e in list)
            {
                if (e == null)
                    continue;
                e.OnUpdate();
            }
        }
    }

    public List<Entity> FindEnemies(Entity owner,int t_Range)
    {
        var ownerFaction = owner.Faction;
        if (EnemyFactionsMap.TryGetValue(ownerFaction, out var enemyFactions))
        {
            var enemies = new List<Entity>();
            foreach (var faction in enemyFactions)
            {
                var entities = GetFractionEntities(faction);
                if (entities != null)
                {
                    enemies.AddRange(entities.Where(
                            entity => entity.GridPosition.Dist(owner.GridPosition) <= t_Range
                        )
                    );
                }
            }
            
            return enemies.OrderBy(entity => entity.GridPosition.Dist(owner.GridPosition)).ToList();
        }

        return null;
    }

    public void OnClearAll()
    {
        foreach (var entities in _entities)
        {
            foreach (var entity in entities.Value)
            {
                OnEntityUnregistered?.Invoke(entity);
                Destroy(entity.gameObject);
            }
        }
        
        _entities.Clear();
        
    }
    
}