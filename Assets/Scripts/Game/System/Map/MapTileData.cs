using UnityEngine;

[System.Serializable]
public class MapTileData
{
    public bool isExplored; // Whether the tile has been explored.
    public bool isVisible; // Whether the tile is visible.
    public Vector3Int localPlace; // The local place of the tile.
}