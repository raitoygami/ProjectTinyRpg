using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class PathNode : IEquatable<PathNode>{
    public PathNode() {
    }
    public PathNode(float x, float z, bool moveable) {
        Position = new Vector2Int((int)x, (int)z);
        _moveable = moveable;
    }

    public PathNode(int x, int z, bool moveable) {
        Position = new Vector2Int(x, z);
        _moveable = moveable;
    }

    public PathNode(int x, int z, bool moveable, Entity entity = null) {
        Position = new Vector2Int(x, z);
        _moveable = moveable;
        Reference = entity;
    }

    /// <summary>Grid position.</summary>
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

    public Entity Reference;

    //public List<Vector2Int> Neighbors;
    public GameObject CheckerBoardRef;

    public string Symbol;
    // AStarNode's costs for pathfinding purposes
    public int gCost;
    public int hCost;

    public LayerMask Layer;

    // the fCost is the gCost+hCost so we can get it directly this way
    public int fCost => gCost + hCost;

    public PathNode ParentNode;
    
    public bool IsMoveable()
    {
        return _moveable;
        // 摆放了道具,
    }

    public void SetMoveable(bool moveable)
    {
        _moveable = moveable;
    }

    private bool _moveable;

    public Vector3 GetLocation()
    {
        return Position.GridToWorld();
    }

    public void Release() {
        if (CheckerBoardRef != null) {
            Object.Destroy(CheckerBoardRef);
        }

        CheckerBoardRef = null;
        //m_OnBoardEntity = null;
    }

    public bool Equals(PathNode other)
    {
        return other != null && other.Position == Position;
    }


}
