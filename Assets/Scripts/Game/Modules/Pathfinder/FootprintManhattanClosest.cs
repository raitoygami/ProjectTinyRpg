using UnityEngine;

/// <summary>
///     Closest pair of cells (by Manhattan distance) between two agent footprints, plus the distance.
/// </summary>
public readonly struct FootprintManhattanClosest
{
    public int Distance { get; }

    /// <summary>Cell on agent A's footprint.</summary>
    public Vector2Int ClosestOnA { get; }

    /// <summary>Cell on agent B's footprint.</summary>
    public Vector2Int ClosestOnB { get; }

    public FootprintManhattanClosest(int distance, int ax, int az, int bx, int bz)
    {
        Distance = distance;
        ClosestOnA = new Vector2Int(ax, az);
        ClosestOnB = new Vector2Int(bx, bz);
    }

    public static FootprintManhattanClosest Invalid => new(int.MaxValue, 0, 0, 0, 0);
}
