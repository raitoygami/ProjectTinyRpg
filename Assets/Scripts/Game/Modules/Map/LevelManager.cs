using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Tilemaps;

public class LevelManager : Singleton<LevelManager>
{
    /// <summary>Lightweight IPathNodeAgent that marks a cell as impassable (no GameObject overhead).</summary>
    private sealed class TilemapBlocker : IPathNodeAgent
    {
        public Vector2Int GridPosition { get; set; }
        public Vector2Int GridSize => Vector2Int.one;
        public int X { get; set; }
        public int Y { get; set; }

        public int GridSizeX => 1;
        public int GridSizeZ => 1;
        public LayerMask Layer { get; set; }
        public bool IsMoveabled(PathCell cell, int goalX, int goalY) => false;
    }

    public async UniTask LoadLevel(string levelName)
    {
        await Addressables.LoadSceneAsync(levelName).ToUniTask();

        var levelLayers = FindFirstObjectByType<LevelLayers>();
        var tilemap = levelLayers != null ? levelLayers.LayerBlocks : null;

        if (tilemap == null)
        {
            Debug.LogError("[LevelManager] LayerBlocks tilemap is null — cannot init PathFinder.");
            return;
        }

        //tilemap.CompressBounds();
        
        // ── Initialize PathFinder from tilemap bounds ──────────────────
        var bounds = tilemap.cellBounds;
        var originX = bounds.xMin;
        var originY = bounds.yMin;
        var width = bounds.xMax - bounds.xMin;
        var height = bounds.yMax - bounds.yMin;

        Debug.Log($"{width}x{height}-{originX}-{originY}");
        
        PathFinder.Instance.InitCells(originX, originY, width, height);
        
        // ── Mark cells with tiles as impassable ────────────────────────
        for (var x = bounds.xMin; x < bounds.xMax; x++)
        for (var y = bounds.yMin; y < bounds.yMax; y++)
        {
            if (tilemap.HasTile(new Vector3Int(x - 1, y - 1, 0)))
            {
                var blocker = new TilemapBlocker
                {
                    GridPosition = new Vector2Int(x, y),
                    Layer = Const.Layer.ObstacleOnly
                };
                PathFinder.Instance.UpdateCell(x, y, blocker);
            }
        }

        // ── Spawn player at PlayerSpawnPoint ───────────────────────────
        var spawnPos = levelLayers.PlayerSpawnPoint != null
            ? levelLayers.PlayerSpawnPoint.transform.position
            : Vector3.zero;
        // CreatePlayer expects grid-container, not world position — convert first
        var spawnGridPos = spawnPos.SnapToGrid();
        var p = EntityManager.Instance.CreatePlayer(spawnGridPos, 100001);
        p.InitAfterLevelLoad();
        Context.Instance.SetPlayer(p);
        CameraManager.Instance.SetFollowTarget(p.transform);
        
        // 初始化
        var activeAfterLoad = levelLayers.ActiveAfterLoad;
        if (activeAfterLoad != null)
        {
            var entities =  activeAfterLoad.GetComponentsInChildren<IDynamicEntity>();
            foreach (var e in entities)
            {
                e.InitAfterLevelLoad();
            }
        }
        
    }

    public void ClearScene()
    {
        EntityManager.Instance.OnClearAll();
        TurnManager.Instance.StopLoop();
        TurnManager.Instance.ClearAll();
        TileSelector.Instance.ClearPath();
    }
    
}
