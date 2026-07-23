using System;
using System.Collections.Generic;
using UnityEngine;

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
public class EnemySpawner : MonoBehaviour
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

    private void Awake()
    {
        SpawnEnemiesAtAllPoints();
    }

    /// <summary>当前 EnemySpawner 所在格子坐标（世界）。</summary>
    public Vector3 SpawnerGridPosition => transform.position.SnapToGrid();

    /// <summary>将相对本 Spawner 的格子偏移转为世界格子坐标。</summary>
    public Vector3 GetAbsoluteGridPosition(Vector3 relativeLocation)
    {
        var baseGrid = SpawnerGridPosition;
        return new Vector3(baseGrid.x + relativeLocation.x, 0, baseGrid.z + relativeLocation.z);
    }

    /// <summary>刷怪点数量。</summary>
    public int SpawnPointCount => spawnPoints != null ? spawnPoints.Count : 0;

    /// <summary>获取指定索引的刷怪点（只读）。</summary>
    public SpawnPoint GetSpawnPoint(int index)
    {
        if (spawnPoints == null || index < 0 || index >= spawnPoints.Count)
            return default;
        return spawnPoints[index];
    }

    /// <summary>在指定刷怪点索引处创建一个 Enemy，使用该刷怪点配置的相对 Location 与 EntityId（t_Entity）。</summary>
    /// <returns>创建的 <see cref="AIEntity"/>，若索引无效或 EntityManager 未配置 enemyPrefab 则返回 null。</returns>
    public AIEntity SpawnEnemyAt(int spawnPointIndex)
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

    /// <summary>在所有刷怪点各创建一个 Enemy，每个刷怪点使用自身配置的 EntityId。</summary>
    /// <returns>创建的 <see cref="AIEntity"/> 列表（跳过失败项）。</returns>
    public List<AIEntity> SpawnEnemiesAtAllPoints()
    {
        var list = new List<AIEntity>();
        if (spawnPoints == null || !EntityManager.HasInstance()) return list;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var enemy = SpawnEnemyAt(i);
            if (enemy != null) list.Add(enemy);
        }
        return list;
    }

    private void OnDrawGizmos()
    {
        var spawnerWorld = transform.position;
        Gizmos.color = spawnerGizmoColor;
        Gizmos.DrawSphere(spawnerWorld, spawnerGizmoRadius);

        if (spawnPoints == null) return;
        Gizmos.color = gizmoColor;
        var baseGrid = SpawnerGridPosition;
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var rel = spawnPoints[i].Location;
            var absGrid = new Vector3(baseGrid.x + rel.x, 0, baseGrid.z + rel.z);
            var worldPos = absGrid.GridToWorld();
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(worldPos, gizmoRadius);

            WorldExtensions.DrawLeashSquareGizmo(worldPos, disengageLeashRange, leashGizmoColor);
        }
    }
}
