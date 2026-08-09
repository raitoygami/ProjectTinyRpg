using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FOVManager : Singleton<FOVManager>
{
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
    private Tilemap _tilemapFOV; // 敌方预警
    private RuleTile _tileFOV;

    public void Setup(TileAssetTable tileAssetTable)
    {
        _root = new GameObject("Root").transform;
        _root.SetParent(transform);
        _root.transform.position = Vector3.zero;

        _tileFOV = tileAssetTable.TileFOV;
        var gridObj = new GameObject("FOV Grid");
        gridObj.transform.SetParent(transform);
        gridObj.transform.localPosition = new Vector3(0.5f, 0.5f, 0);

        _fogGrid = gridObj.AddComponent<Grid>();
        _fogGrid.cellGap = Vector3.zero;
        _fogGrid.cellSize = new Vector3(1, 1, 0);
        _fogGrid.cellLayout = GridLayout.CellLayout.Rectangle;
        _fogGrid.cellSwizzle = GridLayout.CellSwizzle.XYZ;

        var defaultLayer = SortingLayer.NameToID("Default");
        _tilemapFOV = CreateTilemap(gridObj.transform, "Layer Ability Range", defaultLayer, 200);
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

    public void InitFov(Dictionary<Vector3, MapTileData> fovData)
    {
        ClearAll();
        if (fovData == null)
            return;

        foreach (var (location, mapTileData) in fovData)
        {
            if (mapTileData.isExplored) continue;
            var cell = _tilemapFOV.WorldToCell(location);
            _tilemapFOV.SetTile(cell, _tileFOV);
        }
    }

    public void FovCompute(string sceneName, Vector3 location, int viewDistance)
    {
        TileCompute(sceneName, location);
        for (uint octant = 0; octant <= 7; octant++)
            Compute(sceneName, octant, location, viewDistance, 1, new Slope(1, 1), new Slope(0, 1));
    }

    private void Compute(string sceneName, uint octant, Vector3 location, int rangeLimit, int x, Slope top,
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

                var newLocation = new Vector3(tx, ty, 0); // the position of the tile at the top of the column
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

                        Compute(sceneName, octant, location, rangeLimit, x + 1, top, newBottom);
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
    

    private void TileCompute(string sceneName, Vector3 location)
    {
        var mapData = MapManager.Instance.GetMapData(sceneName);
        var tileData = mapData.GetFogTile(location);
        if (tileData == null)
            return;

        var cell = _tilemapFOV.WorldToCell(location);

        _tilemapFOV.SetTileFlags(cell, TileFlags.None);
        _tilemapFOV.SetColor(cell, new Color(1.0f, 1.0f, 1.0f, 0f));
        tileData.isVisible = true;
        tileData.isExplored = true;
    }


    public void ClearAll()
    {
        _tilemapFOV.ClearAllTiles();
    }
}