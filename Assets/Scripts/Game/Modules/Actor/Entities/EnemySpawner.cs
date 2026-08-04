using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 单个刷怪点：相对 EnemySpawner 的格子偏移 + 使用的实体配置 id（t_Entity）。
/// </summary>
[Serializable]
public struct SpawnPoint
{
    [Tooltip("相对 EnemySpawner 的格子坐标偏移（GridBased），x、z 为格坐标偏移。")]
    public Vector3 Location;
    [Tooltip("该刷怪点使用的实体配置 id（DataEntitys 表），0 表示不指定。")]
    public int EntityId;
}

/// <summary>
/// 刷怪点管理：维护若干刷怪点（位置 + 实体配置 id），根据刷怪点创建 Enemy，并用 t_Entity.Attr 初始化 AgentStats，并在 Scene 中用 Gizmos 预览。
/// </summary>
public class EnemySpawner : MonoBehaviour, IDynamicEntity
{
    [Tooltip("刷怪点列表，每项包含格子坐标与实体配置 id（t_Entity）。")]
    [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Tooltip("Gizmos 球体半径，用于预览刷怪点。")]
    [SerializeField] private float gizmoRadius = 0.4f;

    [Tooltip("Gizmos 预览时刷怪点的颜色。")]
    [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 0.2f, 0.6f);

    [Tooltip("Gizmos 预览时 EnemySpawner 本体位置的颜色。")]
    [SerializeField] private Color spawnerGizmoColor = new Color(0.2f, 0.6f, 1f, 0.7f);

    [Tooltip("Gizmos 预览时 EnemySpawner 本体位置的球体半径。")]
    [SerializeField] private float spawnerGizmoRadius = 0.5f;

    [Header("AI 脱离范围（Grid 正方形）")]
    [Tooltip("切比雪夫半径 R：格上与出生格满足 max(|dx|,|dz|)≤R 的正方形区域（边长 2R+1 格）。玩家相对出生格 Dist>R 且追击厌倦时敌人回出生点；0 表示不启用。")]
    [SerializeField] private int disengageLeashRange = 8;

    [Tooltip("Gizmos：脱离正方形线框颜色。")]
    [SerializeField] private Color leashGizmoColor = new Color(0.4f, 1f, 0.4f, 0.35f);

    /// <summary>当前 EnemySpawner 所在格子坐标（世界）。</summary>
    public Vector3 SpawnerGridPosition => transform.position.SnapToGrid();

    /// <summary>将相对本 Spawner 的格子偏移转为世界格子坐标。</summary>


    public void InitAfterLevelLoad()
    {
        var sceneName = SceneManager.GetActiveScene().name;
        if (!gameObject.activeInHierarchy) return;
        var entityName = $"EnemySpawner_{sceneName}_{name}_{transform.position.x}_{transform.position.y}_{transform.position.z}";

        var entityStatData = MapManager.Instance.GetEntityStatData(sceneName, entityName);
        var spawnerState = entityStatData as EntityStatEnemySpawner;
        if (spawnerState is not { HasSpawned: true })
        {
            if (spawnPoints == null || !EntityManager.HasInstance()) return;

            spawnerState = new EntityStatEnemySpawner();
            
            for (var i = 0; i < spawnPoints.Count; i++)
            {
                var enemy = SpawnEnemyAt(i);
                var enemyStat = new EnemyStatData
                {
                    UniqueID = $"{spawnPoints[i].EntityId}_{spawnPoints[i].Location.x}_{spawnPoints[i].Location.y}",
                    EntityId = spawnPoints[i].EntityId,
                    Location = spawnPoints[i].Location,
                    HpLost = 0,
                    IsAlive = true,
                };
                
                enemy.SetEntityState(enemyStat);

                spawnerState.AddEnemyStatData(enemyStat);
            }

            spawnerState.HasSpawned = true;

            MapManager.Instance.SetEntityStatData(sceneName, entityName, spawnerState);
            
            return;
        }

        foreach (var enemyStat in spawnerState.SpawnedEnemies)
        {
            if (!enemyStat.IsAlive) continue;
            var enemy = SpawnEnemy(enemyStat);
            enemy.SetEntityState(enemyStat);            
        }
    }

    private Vector3 GetAbsoluteGridPosition(Vector3 relativeLocation)
    {
        var baseGrid = SpawnerGridPosition;
        return new Vector3(baseGrid.x + relativeLocation.x, baseGrid.y + relativeLocation.y, 0);
    }
    
    private AIEntity SpawnEnemy(EnemyStatData spawnPoint)
    {
        if (!EntityManager.HasInstance())
            return null;
        var enemy = EntityManager.Instance.CreateEnemy(spawnPoint.Location, spawnPoint.EntityId);
        if (enemy != null)
            enemy.SetHomeAnchor(spawnPoint.Location, disengageLeashRange);
        return enemy;
    }
    
    /// <summary>在指定刷怪点索引处创建一个 Enemy，使用该刷怪点配置的相对 Location 与 EntityId（t_Entity）。</summary>
    /// <returns>创建的 <see cref="AIEntity"/>，若索引无效或 EntityManager 未配置 enemyPrefab 则返回 null。</returns>
    private AIEntity SpawnEnemyAt(int spawnPointIndex)
    {
        if (spawnPoints == null || spawnPointIndex < 0 || spawnPointIndex >= spawnPoints.Count)
            return null;
        var point = spawnPoints[spawnPointIndex];
        var absoluteGrid = GetAbsoluteGridPosition(point.Location);
        if (!EntityManager.HasInstance())
            return null;
        var enemy = EntityManager.Instance.CreateEnemy(absoluteGrid, point.EntityId);
        if (enemy != null)
            enemy.SetHomeAnchor(absoluteGrid, disengageLeashRange);
        return enemy;
    }
    
    private void OnDrawGizmos()
    {
        var spawnerWorld = transform.position;
        Gizmos.color = spawnerGizmoColor;
        Gizmos.DrawSphere(spawnerWorld, spawnerGizmoRadius);

        if (spawnPoints == null) return;
        Gizmos.color = gizmoColor;
        var baseGrid = SpawnerGridPosition;
        for (var i = 0; i < spawnPoints.Count; i++)
        {
            var rel = spawnPoints[i].Location;
            var absGrid = new Vector3(baseGrid.x + rel.x, baseGrid.y + rel.y, 0);
            var worldPos = absGrid.SnapToGrid();
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(worldPos, gizmoRadius);

            WorldExtensions.DrawLeashSquareGizmo(worldPos, disengageLeashRange, leashGizmoColor);
        }
    }


}
