using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class FOVManager : Singleton<FOVManager>
{
    public const int PlayerViewDistance = 7;

    private struct Slope
    {
        public Slope(int y, int x)
        {
            Y = y;
            X = x;
        }

        public readonly int Y, X;
    }

    private Grid _fogGrid;
    // 新增：三个 Tilemap 引用

    private Transform _root;
    private Tilemap _tilemapFog; // 敌方预警
    private Tilemap _tilemapView; // 敌方预警
    private RuleTile _tileFog;
    private RuleTile _tileView;
    
    private List<Vector3Int> _visibleTiles = new();
    private List<Vector3Int> _visibleTilesLast = new();
    
    public void Setup(TileAssetTable tileAssetTable)
    {
        _root = new GameObject("Root").transform;
        _root.SetParent(transform);
        _root.transform.position = Vector3.zero;

        _tileFog = tileAssetTable.TileFog;
        _tileView = tileAssetTable.TileView;
        
        var gridObj = new GameObject("FOV Grid");
        gridObj.transform.SetParent(transform);
        gridObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0);

        _fogGrid = gridObj.AddComponent<Grid>();
        _fogGrid.cellGap = Vector3.zero;
        _fogGrid.cellSize = new Vector3(1, 1, 0);
        _fogGrid.cellLayout = GridLayout.CellLayout.Rectangle;
        _fogGrid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        var defaultLayer = SortingLayer.NameToID("Default");
        _tilemapFog = CreateTilemap(gridObj.transform, "Field of View - Fog", defaultLayer, 200);
        _tilemapView = CreateTilemap(gridObj.transform, "Field of View - View", defaultLayer, 199);
        _tilemapView.color = new Color(1, 1, 1, 0.5f);
    }

    private Tilemap CreateTilemap(Transform parent, string tilemapName, int sortingLayerID, int order)
    {
        var child = new GameObject(tilemapName);
        child.transform.SetParent(parent.transform);
        child.transform.localPosition = Vector3.zero;

        var tilemap = child.AddComponent<Tilemap>();
        var tilemapRenderer = child.AddComponent<TilemapRenderer>();

        // 设置排序
        tilemapRenderer.sortingLayerID = sortingLayerID;
        tilemapRenderer.sortingOrder = order;

        // 可选：设置材质（使用默认或者透明材质）
        // renderer.material = ...;

        return tilemap;
    }

    public void InitFov(string sceneName)
    {
        var mapData = MapManager.Instance.GetMapData(sceneName);
        if (mapData == null) return;
        // 遍历地图范围内的所有格子
        for (var x = mapData.OriginX; x <= mapData.OriginX + mapData.Width; x++)
        for (var y = mapData.OriginY; y <= mapData.OriginY + mapData.Height; y++)
        {
            var location = new Vector3Int(x, y, 0);
            // 如果该格子未被探索，则设置黑雾 Tile
            if (mapData.GetFOV(location)) continue;
            var cell = _tilemapFog.WorldToCell(location);
            _tilemapFog.SetTile(cell, _tileFog);
            // 注意：已探索的格子不需要设置 Tile（或者可以设置为空以清除黑雾）
            // 但这里 ClearAll 已经清空了所有，我们只对未探索的设置黑雾，已探索的保持空即可
        }
    }

    public void InitView(string sceneName)
    {
        _visibleTilesLast.Clear();
        _visibleTiles.Clear();
        var mapData = MapManager.Instance.GetMapData(sceneName);
        if (mapData == null) return;
        // 遍历地图范围内的所有格子
        for (var x = mapData.OriginX; x <= mapData.OriginX + mapData.Width; x++)
        for (var y = mapData.OriginY; y <= mapData.OriginY + mapData.Height; y++)
        {
            var location = new Vector3Int(x, y, 0);
            var cell = _tilemapView.WorldToCell(location);
            _tilemapView.SetTile(cell, _tileView);
            _tilemapView.SetColor(cell, Color.white);
            _tilemapView.SetTileFlags(cell, TileFlags.None);
        }
    }

    public void FovCompute(Vector3 location, int viewDistance)
    {
        if (!PlayerManager.HasInstance()) return;
        var locationData = PlayerManager.GetLocation();
        var sceneName = locationData.SceneName;

        _visibleTilesLast = _visibleTiles;
        _visibleTiles = new List<Vector3Int>();
        
        var playerLocation = Vector3Int.FloorToInt(location);
        TileCompute(sceneName, playerLocation);
        for (uint octant = 0; octant <= 7; octant++)
            Compute(sceneName, playerLocation, octant, playerLocation, viewDistance, 1, new Slope(1, 1), new Slope(0, 1));
        
        // 然后在更新
        ViewCompute();
    }

    private void Compute(string sceneName, Vector3Int origin, uint octant, Vector3Int location, int rangeLimit, int x, Slope top,
        Slope bottom)
    {
        for (; (uint)x <= (uint)rangeLimit; x++) // rangeLimit < 0 || x <= rangeLimit
        {
            // compute the Y coordinates where the top vector leaves the column (on the right) and where the bottom vector
            // enters the column (on the left). this equals (x+0.5)*top+0.5 and (x-0.5)*bottom+0.5 respectively, which can
            // be computed like (x+0.5)*top+0.5 = (2(x+0.5)*top+1)/2 = ((2x+1)*top+1)/2 to avoid floating point math
            var topY = top.X == 1
                ? x
                : ((x * 2 + 1) * top.Y + top.X - 1) / (top.X * 2); // the rounding is a bit tricky, though
            var bottomY = bottom.Y == 0 ? 0 : ((x * 2 - 1) * bottom.Y + bottom.X) / (bottom.X * 2);

            var wasOpaque = -1; // 0:false, 1:true, -1:not applicable
            // compute the top and bottom vectors for the next column
            for (var y = topY; y >= bottomY; y--)
            {
                var tx = location.x;
                var ty = location.y;
                // compute the coordinates of the tile at the top of the column
                switch (octant)
                {
                    case 0:
                        tx += x;
                        ty -= y;
                        break;
                    case 1:
                        tx += y;
                        ty -= x;
                        break;
                    case 2:
                        tx -= y;
                        ty -= x;
                        break;
                    case 3:
                        tx -= x;
                        ty -= y;
                        break;
                    case 4:
                        tx -= x;
                        ty += y;
                        break;
                    case 5:
                        tx -= y;
                        ty += x;
                        break;
                    case 6:
                        tx += y;
                        ty += x;
                        break;
                    case 7:
                        tx += x;
                        ty += y;
                        break;
                }
                
                var newLocation = new Vector3Int(tx, ty, 0); // the position of the tile at the top of the column
                if (!origin.IsWithinVisionRange(newLocation, rangeLimit))
                    continue;
                
                TileCompute(sceneName, newLocation);
                // NOTE: use the next line instead if you want the algorithm to be symmetrical

                var isOpaque = IsBlockView(newLocation);

                if (x == rangeLimit) continue;
                
                if (isOpaque)
                {
                    if (wasOpaque ==
                        0) // if we found a transition from clear to opaque, this sector is done in this column, so
                    {
                        // adjust the bottom vector upwards and continue processing it in the next column.
                        var newBottom =
                            new Slope(y * 2 + 1,
                                x * 2 - 1); // (x*2-1, y*2+1) is a vector to the top-left of the opaque tile
                        if (y == bottomY)
                        {
                            bottom = newBottom;
                            break;
                        } // don't recurse unless we have to

                        Compute(sceneName, origin, octant, location, rangeLimit, x + 1, top, newBottom);
                    }

                    wasOpaque = 1;
                }
                else // adjust top vector downwards and continue if we found a transition from opaque to clear
                {
                    // (x*2+1, y*2+1) is the top-right corner of the clear tile (i.e. the bottom-right of the opaque tile)
                    if (wasOpaque > 0) top = new Slope(y * 2 + 1, x * 2 + 1);
                    wasOpaque = 0;
                }
            }

            if (wasOpaque != 0)
                break; // if the column ended in a clear tile, continue processing the current sector
        }
    }

    private bool IsBlockView(Vector3 location)
    {
        var cell = PathFinder.Instance.GetCell(location.x, location.y);

        if (cell?.Logical == null)
            return false;

        // 【核心逻辑】只要有任何一格包含 ObstacleForNavi 就不能停留
        return (Const.Layer.LayerFogComputeFOV.value & cell.Logical.Layer.value) != 0;
    }

    private void TileCompute(string sceneName, Vector3Int location)
    {
        if (!MapManager.HasInstance())
            return;
        
        if (!_visibleTiles.Contains(location))
            _visibleTiles.Add(location);
        
        var mapData = MapManager.Instance.GetMapData(sceneName);
        var hasFOV = mapData.HasFOV(location);
        if (!hasFOV)
            return;

        var cell = _tilemapFog.WorldToCell(location);

        _tilemapFog.SetTileFlags(cell, TileFlags.None);
        _tilemapFog.SetColor(cell, new Color(1.0f, 1.0f, 1.0f, 0f));
        mapData.SetFOV(location, true);

    }


    private void ViewCompute()
    {
        var added = _visibleTiles.Except(_visibleTilesLast).ToList();
        var removed = _visibleTilesLast.Except(_visibleTiles).ToList();
        
        foreach (var location in added)
        {
            var cell = _tilemapView.WorldToCell(location);
            _tilemapView.SetTileFlags(cell, TileFlags.None);
            _tilemapView.SetColor(cell, Color.clear);
        }

        foreach (var location in removed)
        {
            var cell = _tilemapView.WorldToCell(location);
            _tilemapView.SetTileFlags(cell, TileFlags.None);
            _tilemapView.SetColor(cell, Color.white);
        }
        
    }
    
    public bool IsVisibility(Vector3Int location)
    {
        return _visibleTiles.Contains(location);
    }
    
    public void InitVisibility()
    {
        if (!EntityManager.HasInstance()) return;
        
        var entitiesTable = EntityManager.Instance.GetEntitiesTable();
        foreach (var (faction, entities) in entitiesTable)
        {
            if (faction != EntityFaction.Enemy) continue;
            foreach (var entity in entities)
            {
                var agentAnimation = entity.GetComponent<AgentAnimations>();
                if (agentAnimation == null)
                    continue;
                var location = entity.GridPosition;
                agentAnimation.SetVisibility(IsVisibility(Vector3Int.FloorToInt(location)));
            }
        }
    }
    
    private List<Vector3Int> GetUpdatedTiles()
    {
        var added = _visibleTiles.Except(_visibleTilesLast).ToList();
        var removed = _visibleTilesLast.Except(_visibleTiles).ToList();
        added.AddRange(removed);
        
        return added;
    }
    
    public void PlayerVisibilityChanged()
    {
        if (!PathFinder.HasInstance()) return;

        var updateTiles = GetUpdatedTiles();
        foreach (var location in updateTiles)
        {
            var cell = PathFinder.Instance.GetCell(location.x, location.y);
            var target = cell?.Logical;
            var entity = target as Entity;
            if (entity == null) continue;
            var agentAnimation = entity.GetComponent<AgentAnimations>();
            if (agentAnimation == null)
                continue;
            var visible = IsVisibility(location);
            if (visible)
                agentAnimation.Fadein().Forget();
            else
                agentAnimation.Fadeout().Forget();
        }
        
    }

    public void RefreshVisibility(Entity entity, Vector3Int nextLocation)
    {
        var agentAnimation = entity.GetComponent<AgentAnimations>();
        if (agentAnimation == null) return;
        
        var visible = IsVisibility(nextLocation);
        if (visible)
            agentAnimation.Fadein().Forget();
        else
            agentAnimation.Fadeout().Forget();
    }
    
    public void ClearAll()
    {
        _tilemapFog.ClearAllTiles();
        _tilemapView.ClearAllTiles();
    }
}