using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SceneLoader : Singleton<SceneLoader>
{
    // 场景切换的时候调用
    public class SceneChangeEvt : EventArgs
    {
    }
    
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

        // CreatePlayer expects grid-container, not world position — convert first
        var entityID = PlayerManager.Instance.GetEntityID();
        var location = PlayerManager.Instance.GetWorldLocation();
        var p = EntityManager.Instance.CreatePlayer(location, entityID);
        p.InitAfterLevelLoad();
        Context.Instance.SetPlayer(p);
        CameraManager.Instance.SetFollowTarget(p.transform);
        await p.FirstBindAfterInst();
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
