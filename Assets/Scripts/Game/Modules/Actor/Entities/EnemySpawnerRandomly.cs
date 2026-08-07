using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
///     在自身所在 Grid 的直径为 Range（奇数）的范围内随机生成 n 个 Enemy，实体配置从 List&lt;entityId&gt;（t_Entity）中随机选取，位置不重复，且 n &lt; Range*Range。
/// </summary>
public class EnemySpawnerRandomly : PubSubActor
{
    [Tooltip("生成区域直径（格子数），x、z 方向均为该长度；会自动取为奇数（如 4→3）。")]
    [SerializeField] private int range = 5;

    /// <summary>实际使用的范围直径（保证为奇数，至少 1）。</summary>
    private int EffectiveRange => Mathf.Max(1, (range - 1) | 1);

    [Tooltip("要生成的 Enemy 数量，实际取值 min(n, Range*Range-1)。")] [SerializeField]
    private int count = 10;

    [Tooltip("每个 Enemy 的实体配置 id（t_Entity）从此列表中随机选取。")] [SerializeField]
    private List<int> entityIds = new();

    [Tooltip("随机种子，0 表示使用随机。")] [SerializeField]
    private int seed;

    [Header("Gizmos")] [Tooltip("EnemySpawnerRandomly 自身格子位置的 Gizmos 颜色。")] [SerializeField]
    private Color spawnerGizmoColor = new(0.2f, 0.6f, 1f, 0.8f);

    [Tooltip("EnemySpawnerRandomly 自身格子位置的球体半径。")] [SerializeField]
    private float spawnerGizmoRadius = 0.5f;

    [Tooltip("生成范围边框的 Gizmos 颜色。")] [SerializeField]
    private Color rangeGizmoColor = new(1f, 0.5f, 0f, 0.8f);

    [Header("AI 脱离范围（Grid 正方形）")]
    [Tooltip("切比雪夫半径 R：格上与各敌出生格满足 max(|dx|,|dz|)≤R 的正方形（边长 2R+1 格）。玩家 Dist>R 且追击厌倦时回出生点；0 不启用。")]
    [SerializeField]
    private int disengageLeashRange = 8;

    [Tooltip("Gizmos：脱离正方形线框颜色（示意刷怪中心；运行时每怪以自身出生格为心）。")] [SerializeField]
    private Color leashGizmoColor = new(0.4f, 1f, 0.4f, 0.35f);

    private void Awake()
    {
        this.SubscribeGlobal<MapLoader.MapChangedEvt>(OnSceneChanged);
    }

    private UniTask OnSceneChanged(MapLoader.MapChangedEvt arg)
    {
        SpawnRandomly();
        return UniTask.CompletedTask;
    }

    /// <summary>
    ///     在 Range 范围内随机生成不超过 min(count, Range*Range-1) 个 Enemy，位置不重复，模板随机。
    /// </summary>
    public void SpawnRandomly()
    {
        if (!EntityManager.HasInstance()) return;

        var diameter = EffectiveRange;
        var halfExtent = (diameter - 1) / 2;
        var maxCount = Mathf.Max(0, diameter * diameter - 1);
        var n = Mathf.Clamp(count, 0, maxCount);
        if (n <= 0) return;

        var baseGrid = transform.position.SnapToGrid();
        var bx = Mathf.RoundToInt(baseGrid.x);
        var bz = Mathf.RoundToInt(baseGrid.z);

        var positions = new List<(int x, int z)>();
        for (var x = bx - halfExtent; x <= bx + halfExtent; x++)
        for (var z = bz - halfExtent; z <= bz + halfExtent; z++)
            positions.Add((x, z));

        if (seed != 0) Random.InitState(seed);
        Shuffle(positions);

        int PickEntityId()
        {
            if (entityIds == null || entityIds.Count == 0) return 0;
            return entityIds[Random.Range(0, entityIds.Count)];
        }
        
        for (var i = 0; i < n && i < positions.Count; i++)
        {
            var p = positions[i];
            var location = new Vector3(p.x, 0, p.z);
            var entityId = PickEntityId();
            var enemy = EntityManager.Instance.CreateEnemy(location, entityId);
            
            if (enemy != null)
            {
                enemy.name = $"Enemy {i} : - {entityId} ";
                enemy.SetHomeAnchor(location, transform.position.SnapToGrid(), disengageLeashRange);
            }
        }
        
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnDrawGizmos()
    {
        var baseGrid = transform.position.SnapToGrid();
        var centerWorld = baseGrid.GridToWorld();

        Gizmos.color = spawnerGizmoColor;
        Gizmos.DrawSphere(centerWorld, spawnerGizmoRadius);

        var diameter = EffectiveRange;
        if (diameter <= 0) return;
        Gizmos.color = rangeGizmoColor;
        var sizeX = diameter;
        var sizeY = diameter * WorldExtensions.WorldToGridScale;
        var size = new Vector3(sizeX, sizeY, 0.01f);
        Gizmos.matrix = Matrix4x4.TRS(centerWorld, Quaternion.identity, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, size);
        Gizmos.matrix = Matrix4x4.identity;

        WorldExtensions.DrawLeashSquareGizmo(centerWorld, disengageLeashRange, leashGizmoColor);
    }
}