using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     A* pathfinder operating on a 2D grid of <see cref="PathCell" /> / <see cref="IPathNodeAgent" />.
/// </summary>
public class PathFinder : Singleton<PathFinder>
{
    private readonly List<(int ax, int ay)> _anchorOpen = new();
    private readonly HashSet<(int, int)> _anchorClosed = new();
    private readonly HashSet<(int, int)> _anchorOpenSet = new();
    private readonly Dictionary<(int, int), int> _anchorG = new();
    private readonly Dictionary<(int, int), (int px, int pz)> _anchorParent = new();

    /// <summary>Cardinal neighbour offsets in grid space.</summary>
    private static readonly Vector2Int[] NeighbourOffsets =
    {
        new(-1, 0), new(1, 0),
        new(0, -1), new(0, 1),
        new(-1, -1), new(-1, 1),
        new(1, 1), new(1, -1)
    };

    public PathCell2D Cell { get; } = new();

    // ── Init / Update ───────────────────────────────────────────────────

    public void InitCells(int originX, int originY, int width, int height, bool createNodes = true)
    {
        Cell.Init(originX, originY, width, height, createNodes);
    }

    public void UpdateCell(int x, int z, IPathNodeAgent logical)
    {
        Cell.UpdateCell(x, z, logical);
    }

    public void UpdateCell(Vector2Int pos, IPathNodeAgent logical)
    {
        Cell.UpdateCell(pos.x, pos.y, logical);
    }

    public void ClearLogical(IPathNodeAgent logical)
    {
        if (logical == null)
            return;
        Cell.ClearLogical(logical);
    }

    // ── GetNode ─────────────────────────────────────────────────────────

    public PathCell GetCell(int x, int y)
    {
        return Cell.Get(x, y);
    }

    public PathCell GetCell(Vector2Int pos)
    {
        return Cell.Get(pos);
    }

    public PathCell GetCell(float x, float z)
    {
        return Cell.Get((int)x, (int)z);
    }

    // ── Move ────────────────────────────────────────────────────────────

    public void Move(int x1, int z1, int x2, int z2, IPathNodeAgent nodeAgent)
    {
        Cell.Move(x1, z1, x2, z2, nodeAgent);
    }

    public void Move(Vector2Int from, Vector2Int to, IPathNodeAgent nodeAgent)
    {
        Cell.Move(from.x, from.y, to.x, to.y, nodeAgent);
    }

    // ── Navigate (A*) ───────────────────────────────────────────────────

    public List<Vector2Int> Navigate(IPathNodeAgent mover, int sx, int sz, int tx, int tz,
        int range = -1, List<Vector2Int> pathOut = null)
    {
        return Navigate(mover, new Vector2Int(sx, sz), new Vector2Int(tx, tz), range, pathOut);
    }

    public List<Vector2Int> Navigate(IPathNodeAgent mover, Vector2Int start, Vector2Int goal,
        int range = -1, List<Vector2Int> pathOut = null)
    {
        if (mover == null)
            return null;

        if (!CanPlaceFootprint(mover, start.x, start.y, goal.x, goal.y) ||
            !CanPlaceFootprint(mover, goal.x, goal.y, goal.x, goal.y))
            return null;

        _anchorOpen.Clear();
        _anchorClosed.Clear();
        _anchorOpenSet.Clear();
        _anchorG.Clear();
        _anchorParent.Clear();

        var startKey = (start.x, start.y);
        var goalKey = (goal.x, goal.y);

        _anchorG[startKey] = 0;
        _anchorOpen.Add(startKey);
        _anchorOpenSet.Add(startKey);

        while (_anchorOpen.Count > 0)
        {
            var current = PopLowestAnchor(goal.x, goal.y);
            _anchorClosed.Add(current);

            if (current.ax == goal.x && current.ay == goal.y)
                return RetraceAnchorPath(startKey, goalKey, pathOut);

            var cx = current.ax;
            var cy = current.ay;

            foreach (var offset in NeighbourOffsets)
            {
                var nax = cx + offset.x;
                var naz = cy + offset.y;
                if (range > 0 && Dist(start.x, start.y, nax, naz) > range)
                    continue;

                var neighKey = (nax, naz);
                if (_anchorClosed.Contains(neighKey))
                    continue;

                var canEnter = CanPlaceFootprint(mover, nax, naz, goal.x, goal.y);

                var isDiagonal = offset.x != 0 && offset.y != 0;
                if (isDiagonal && canEnter)
                {
                    var horizontalOk = (nax == goal.x && cy == goal.y) ||
                                       CanPlaceFootprint(mover, cx + offset.x, cy, goal.x, goal.y);
                    var verticalOk = (cx == goal.x && naz == goal.y) ||
                                     CanPlaceFootprint(mover, cx, cy + offset.y, goal.x, goal.y);
                    canEnter = horizontalOk || verticalOk;
                }

                if (!canEnter)
                    continue;

                var moveCost = offset.x != 0 && offset.y != 0 ? 14 : 10;
                var tentativeG = _anchorG[(current.ax, current.ay)] + moveCost;
                if (_anchorG.TryGetValue(neighKey, out var oldG) && tentativeG >= oldG)
                    continue;

                _anchorG[neighKey] = tentativeG;
                _anchorParent[neighKey] = (cx, cy);
                if (_anchorOpenSet.Add(neighKey))
                    _anchorOpen.Add(neighKey);
            }
        }

        return null;
    }

    // ── CanPlaceFootprint ───────────────────────────────────────────────

    public bool CanPlaceFootprint(IPathNodeAgent mover, int anchorX, int anchorZ, int goalX, int goalY)
    {
        var gw = mover.GridSize.x;
        var gh = mover.GridSize.y;
        for (var ox = 0; ox < gw; ox++)
        for (var oy = 0; oy < gh; oy++)
        {
            var gx = anchorX + ox;
            var gy = anchorZ + oy;
            if (!Cell.Contains(gx, gy))
                return false;
            var cell = Cell.Get(gx, gy);
            if (!IsWalkableCell(cell, mover, goalX, goalY))
                return false;
        }

        return true;
    }

    public bool CanPlaceFootprint(IPathNodeAgent mover, Vector2Int anchor, Vector2Int goal)
    {
        return CanPlaceFootprint(mover, anchor.x, anchor.y, goal.x, goal.y);
    }

    // ── A* internals ────────────────────────────────────────────────────

    private (int ax, int ay) PopLowestAnchor(int gfx, int gfy)
    {
        var best = 0;
        var bestF = int.MaxValue;
        var bestH = int.MaxValue;
        for (var i = 0; i < _anchorOpen.Count; i++)
        {
            var key = _anchorOpen[i];
            var g = _anchorG[key];
            var h = HeuristicAnchor(key.Item1, key.Item2, gfx, gfy);
            var f = g + h;
            if (f < bestF || (f == bestF && h < bestH))
            {
                best = i;
                bestF = f;
                bestH = h;
            }
        }

        var picked = _anchorOpen[best];
        _anchorOpen.RemoveAt(best);
        _anchorOpenSet.Remove(picked);
        return (picked.Item1, picked.Item2);
    }

    private static int HeuristicAnchor(int ax, int az, int gx, int gz)
    {
        var dx = Mathf.Abs(ax - gx);
        var dz = Mathf.Abs(az - gz);
        return 10 * (dx + dz) + 4 * Mathf.Min(dx, dz);
    }

    private List<Vector2Int> RetraceAnchorPath((int sx, int sz) start, (int tx, int tz) goal,
        List<Vector2Int> pathOut)
    {
        if (pathOut == null)
            pathOut = new List<Vector2Int>();
        else
            pathOut.Clear();

        var startKey = (start.sx, start.sz);
        var cur = (goal.tx, goal.tz);
        while (cur != startKey)
        {
            pathOut.Add(new Vector2Int(cur.Item1, cur.Item2));
            if (!_anchorParent.TryGetValue(cur, out var p))
                return null;
            cur = p;
        }

        pathOut.Reverse();
        return pathOut;
    }

    // ── Walkability ─────────────────────────────────────────────────────

    public static bool IsWalkableCell(PathCell cell, IPathNodeAgent mover, int goalX, int goalY)
    {
        if (cell == null) return false;
        if (cell.Logical == null) return true;
        if (mover != null && ReferenceEquals(cell.Logical, mover)) return true;
        if (mover == null)
            return cell.Logical.IsMoveable(cell, goalX, goalY);
        return mover.IsMoveable(cell, goalX, goalY);
    }

    public static bool IsWalkableCellForce(PathCell cell, IPathNodeAgent mover)
    {
        if (cell == null) return false;
        return IsWalkableCell(cell, mover, 100000, 100000);
    }
    
    /// <summary>No separate goal — uses the cell's own position as the goal.</summary>
    public static bool IsWalkableCell(PathCell cell, IPathNodeAgent mover)
    {
        if (cell == null) return false;
        return IsWalkableCell(cell, mover, cell.X, cell.Y);
    }

    /// <summary>
    ///     Is the cell inside the goal footprint rectangle for the given agent?
    /// </summary>
    public static bool IsCellInGoalFootprint(IPathNodeAgent nodeAgent, PathCell cell, int goalAnchorX, int goalAnchorY)
    {
        var sizeX = nodeAgent.GridSize.x;
        var sizeY = nodeAgent.GridSize.y;

        return cell.X >= goalAnchorX && cell.X < goalAnchorX + sizeX &&
               cell.Y >= goalAnchorY && cell.Y < goalAnchorY + sizeY;
    }

    // ── Distance helpers ────────────────────────────────────────────────

    private static int Dist(int x1, int sz, int x2, int z2)
    {
        return Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(z2 - sz));
    }

}