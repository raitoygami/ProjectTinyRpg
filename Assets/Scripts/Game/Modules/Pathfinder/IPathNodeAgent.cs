using UnityEngine;


public interface IPathNodeAgent
{
    /// <summary>Grid position (column, row).</summary>
    Vector2Int GridLocation { get; set; }

    /// <summary>Footprint size in grid cells (width, height), at least (1,1).</summary>
    Vector2Int GridSize { get; }

    /// <summary>Backward-compat: grid column.</summary>
    int X { get; set; }

    /// <summary>Backward-compat: grid row.</summary>
    int Y { get; set; }

    /// <summary>Backward-compat: footprint width.</summary>
    int GridSizeX { get; }

    /// <summary>Backward-compat: footprint height.</summary>
    int GridSizeZ { get; }

    LayerMask Layer { get; set; }

    /// <summary>
    /// Called by the pathfinder on the mover: can the footprint be placed on <paramref name="cell"/>?
    /// <paramref name="goalX"/> and <paramref name="goalY"/> are the pathfinding goal anchor.
    /// </summary>
    public bool IsMoveable(PathCell cell, int goalX, int goalY);
    public bool BlockVision();

}

public sealed class PathCell
{
    /// <summary>Grid position of this cell.</summary>
    public Vector2Int Position;

    /// <summary>Backward-compat: grid column.</summary>
    public int X
    {
        get => Position.x;
        set => Position.x = value;
    }

    /// <summary>Backward-compat: grid row.</summary>
    public int Y
    {
        get => Position.y;
        set => Position.y = value;
    }

    public int gCost;
    public int hCost;
    public int fCost => gCost + hCost;

    /// <summary>
    /// The logical agent occupying this cell (may be shared across multiple cells for multi-cell footprints).
    /// </summary>
    public IPathNodeAgent Logical;
}
