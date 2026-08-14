using System;
using Cysharp.Threading.Tasks;
using JSAM;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class MapLoader : Singleton<MapLoader>
{
    // 场景切换的时候调用
    public class MapChangedEvt : EventArgs
    {
    }
    
    /// <summary>Lightweight IPathNodeAgent that marks a cell as impassable (no GameObject overhead).</summary>
    private sealed class TilemapBlocker : IPathNodeAgent
    {
        public Vector2Int GridLocation { get; set; }
        public Vector2Int GridSize => Vector2Int.one;
        public int X { get; set; }
        public int Y { get; set; }

        public int GridSizeX => 1;
        public int GridSizeY => 1;
        public LayerMask Layer { get; set; }
        public bool IsMoveable(PathCell cell, int goalX, int goalY) => false;
        public bool BlockVision()
        {
            return (Const.Layer.ObstacleOnly.value & Layer.value) != 0;
        }
    }

    public async UniTask Load(string sceneName)
    {
        var mapInfo = MapManager.Instance.GetMapInfo(sceneName);
        if (mapInfo == null)
        {
            Debug.LogError($"[MapLoader] Wrong there is no map info of : {sceneName}.");
            return;
        }
            
        await Addressables.LoadSceneAsync(mapInfo.AddressableName).ToUniTask();

        // 将缓存全部清掉
        PoolManager.Instance.ClearAll();
        
        var mapLayers = FindFirstObjectByType<MapLayers>();
        var tilemap = mapLayers != null ? mapLayers.LayerBlocks : null;

        if (tilemap == null)
        {
            Debug.LogError("[MapLoader] LayerBlocks tilemap is null — cannot init PathFinder.");
            return;
        }

        //tilemap.CompressBounds();
        
        // ── Initialize PathFinder from tilemap bounds ──────────────────
        var bounds = tilemap.cellBounds;
        var originX = bounds.xMin;
        var originY = bounds.yMin;
        var width = bounds.xMax - bounds.xMin;
        var height = bounds.yMax - bounds.yMin;

        Debug.Log($"{sceneName}-{width}x{height}-{originX}-{originY}");
        
        PathFinder.Instance.InitCells(originX, originY, width, height);
        
        // 全图初始化fov
        var mapData = MapManager.Instance.GetMapData(sceneName);
        mapData.InitFogTiles(originX - 2, originY - 2, width + 4, height + 4);
        FOVManager.Instance.ClearAll();
        FOVManager.Instance.InitView(sceneName);

        var tileAssetTable = PreloadSettings.Instance.GetTileAssetTable();

        // ── Mark cells with tiles as impassable ────────────────────────
        for (var x = bounds.xMin; x < bounds.xMax; x++)
        for (var y = bounds.yMin; y < bounds.yMax; y++)
        {
            var cell = tilemap.WorldToCell(new Vector3(x, y, 0));
            if (!tilemap.HasTile(cell)) continue;
            
            var tile = tilemap.GetTile(cell);
            var layerMask = tileAssetTable.GetLayerByTile(tile);
            
            var blocker = new TilemapBlocker
            {
                GridLocation = new Vector2Int(x, y),
                Layer = layerMask,
            };
            PathFinder.Instance.UpdateCell(x, y, blocker);
        }

        var blockPropRoot = mapLayers.BlockProps;
        if (blockPropRoot != null)
        {
            for (var i = 0; i < blockPropRoot.childCount; i++)
            {
                var block = blockPropRoot.GetChild(i);
                var cell = Vector3Int.FloorToInt(block.position);
                var blocker = new TilemapBlocker
                {
                    GridLocation = new Vector2Int(cell.x, cell.y),
                    Layer = Const.Layer.BlockOnly,
                };
                PathFinder.Instance.UpdateCell(cell.x, cell.y, blocker);
                // 在这里处理每个一级子节点
            }
        }
        
        // 新加载的地图 清理掉所有敌方预警
        GridIndicatorManager.Instance.ClearAll();
        // 初始化
        var activeAfterLoad = mapLayers.ActiveAfterLoad;
        if (activeAfterLoad != null)
        {
            var entities =  activeAfterLoad.GetComponentsInChildren<IDynamicEntity>();
            foreach (var e in entities)
            {
                e.InitAfterLevelLoad();
            }
        }

        // CreatePlayer expects grid-container, not world position — convert first
        var entityID = PlayerManager.Instance.GetEntityID();
        var location = PlayerManager.Instance.GetCurrentLocation();
        var p = EntityManager.Instance.CreatePlayer(location, entityID);
        p.InitAfterLevelLoad();
        Context.Instance.SetPlayer(p);
        CameraManager.Instance.SetFollowTarget(p.transform);
        CameraManager.Instance.SetMapBound(mapLayers.CameraBound);
        
        await p.FirstBindAfterInst();
        if (mapInfo.MapType == MapConfig.MapType.WorldChunk)
        {
            PlayerManager.Instance.SetWorld(sceneName);
            PlayerManager.Instance.SetWorldLocation(p.transform.position);
        }

        // 初始化
        FOVManager.Instance.InitFov(sceneName);
        FOVManager.Instance.InitVisibility();
        
        AudioManager.FadeMainMusicOut(1f);
        AudioManager.FadeMusicIn(mapInfo.MainMusic, 1f, true);
    }

    public void OnCombatStart(float fadeOut)
    {
        AudioManager.FadeMainMusicOut(fadeOut);
    }
    
    public void OnCombatEnded(float fadeIn)
    {
        var mapName = PlayerManager.Instance.GetCurrentMap();
        var mapInfo = MapManager.Instance.GetMapInfo(mapName);
        if (mapInfo == null)
        {
            Debug.LogError($"[MapLoader] Wrong there is no map info of : {mapName}.");
            return;
        }
        AudioManager.FadeMusicIn(mapInfo.MainMusic, fadeIn, true);
    }
    
    public void ClearScene()
    {
        EntityManager.Instance.OnClearAll();
        TurnManager.Instance.StopLoop();
        TurnManager.Instance.ClearAll();
        GridIndicatorManager.Instance.HideCursorMark();
    }
    
}
