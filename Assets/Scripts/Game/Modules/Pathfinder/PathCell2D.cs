using System.Collections.Generic;
using UnityEngine;

public class PathCell2D
{
    private readonly Dictionary<Vector2Int, PathCell> _cells = new Dictionary<Vector2Int, PathCell>();

    public int OriginX { get; private set; }
    public int OriginZ { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Vector2Int Origin => new Vector2Int(OriginX, OriginZ);
    public Vector2Int Size => new Vector2Int(Width, Height);

    // ── Contains（可选保留，用于“当前管理范围”检查） ─────────────────────
    public bool Contains(int x, int z)
    {
        var ix = x - OriginX;
        var iz = z - OriginZ;
        return ix >= 0 && ix < Width && iz >= 0 && iz < Height;
    }

    public bool Contains(Vector2Int pos) => Contains(pos.x, pos.y);

    // ── Get / Set ───────────────────────────────────────────────────────
    public PathCell Get(int x, int z)
    {
        var key = new Vector2Int(x, z);
        _cells.TryGetValue(key, out var cell);
        return cell;
    }

    public PathCell Get(Vector2Int pos) => Get(pos.x, pos.y);

    public void Set(int x, int z, PathCell node)
    {
        var key = new Vector2Int(x, z);
        if (node == null)
            _cells.Remove(key);
        else
            _cells[key] = node;
    }

    public void Set(Vector2Int pos, PathCell node) => Set(pos.x, pos.y, node);

    // ── Init ────────────────────────────────────────────────────────────
    public void Init(int originX, int originZ, int width, int height, bool createNodes = true)
    {
        _cells.Clear();

        OriginX = originX;
        OriginZ = originZ;
        Width = width;
        Height = height;

        if (!createNodes) return;

        for (var ix = 0; ix < width; ix++)
        for (var iz = 0; iz < height; iz++)
        {
            var x = originX + ix;
            var z = originZ + iz;
            var pos = new Vector2Int(x, z);
            
            _cells[pos] = new PathCell 
            { 
                Position = pos, 
                Logical = null 
            };
        }
    }

    // ── UpdateCell ──────────────────────────────────────────────────────
    public void UpdateCell(int x, int z, IPathNodeAgent logical)
    {
        if (logical == null) return;

        var w = Mathf.Max(1, logical.GridSize.x);
        var h = Mathf.Max(1, logical.GridSize.y);

        for (var ox = 0; ox < w; ox++)
        for (var oz = 0; oz < h; oz++)
        {
            var gx = x + ox;
            var gz = z + oz;
            var pos = new Vector2Int(gx, gz);

            if (!_cells.TryGetValue(pos, out var cell))
            {
                cell = new PathCell { Position = pos, Logical = logical };
                _cells[pos] = cell;
            }
            else
            {
                cell.Logical = logical;
            }
        }
    }

    public void UpdateCell(Vector2Int pos, IPathNodeAgent logical)
        => UpdateCell(pos.x, pos.y, logical);

    // ── ClearLogical ────────────────────────────────────────────────────
    public void ClearLogical(IPathNodeAgent logical)
    {
        if (logical == null) return;

        var w = Mathf.Max(1, logical.GridSize.x);
        var h = Mathf.Max(1, logical.GridSize.y);
        var x = logical.GridLocation.x;
        var z = logical.GridLocation.y;

        for (int ox = 0; ox < w; ox++)
        for (int oz = 0; oz < h; oz++)
        {
            var pos = new Vector2Int(x + ox, z + oz);
            if (_cells.TryGetValue(pos, out var cell) && 
                ReferenceEquals(cell.Logical, logical))
            {
                cell.Logical = null;
            }
        }
    }

    // ── Move ────────────────────────────────────────────────────────────
    public void Move(int x1, int z1, int x2, int z2, IPathNodeAgent nodeAgent)
    {
        if (nodeAgent == null) return;

        var w = Mathf.Max(1, nodeAgent.GridSize.x);
        var h = Mathf.Max(1, nodeAgent.GridSize.y);

        // Clear old
        for (var ox = 0; ox < w; ox++)
        for (var oz = 0; oz < h; oz++)
        {
            var pos = new Vector2Int(x1 + ox, z1 + oz);
            if (_cells.TryGetValue(pos, out var cell) && 
                ReferenceEquals(cell.Logical, nodeAgent))
            {
                cell.Logical = null;
            }
        }

        nodeAgent.GridLocation = new Vector2Int(x2, z2);

        // Set new
        for (var ox = 0; ox < w; ox++)
        for (var oz = 0; oz < h; oz++)
        {
            var pos = new Vector2Int(x2 + ox, z2 + oz);
            if (!_cells.TryGetValue(pos, out var cell))
            {
                cell = new PathCell { Position = pos };
                _cells[pos] = cell;
            }
            cell.Logical = nodeAgent;
        }
    }

    public void Move(Vector2Int from, Vector2Int to, IPathNodeAgent nodeAgent)
        => Move(from.x, from.y, to.x, to.y, nodeAgent);
}