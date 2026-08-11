using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Entity : PubSubActor, IPathNodeAgent, IDynamicEntity
{
    [SerializeField] private bool _updateNavigation = true;
    public EntityFaction Faction;

    /// <summary>World-space grid-container position (gridX, 0, gridZ).</summary>
    public Vector3 GridPosition => transform ? transform.position.SnapToGrid() : Vector3.zero;

    // ── IPathNodeAgent implementation ───────────────────────────────────

    Vector2Int IPathNodeAgent.GridLocation
    {
        get => new(X, Y);
        set
        {
            X = value.x;
            Y = value.y;
        }
    }

    Vector2Int IPathNodeAgent.GridSize => new(GridSizeX, GridSizeZ);
    
    /// <summary>Grid column.</summary>
    public int X { get; set; }

    /// <summary>Grid row.</summary>
    public int Y { get; set; }

    /// <summary>Footprint width in grid cells (≥1).</summary>
    public int GridSizeX { get; protected set; } = 1;

    /// <summary>Footprint height in grid cells (≥1).</summary>
    public int GridSizeZ { get; protected set; } = 1;

    public LayerMask Layer { get; set; }

    // ── Lifecycle ───────────────────────────────────────────────────────
    
    /// <summary>
    /// Called by <see cref="EntityManager"/> each frame. Override in subclasses.
    /// Entities removed/destroyed during iteration may still receive this call for the current frame.
    /// </summary>
    public virtual void OnUpdate()
    {
    }

    protected virtual bool IsWalkable(PathCell cell, int goalX, int goalY)
    {
        return false;
    }

    public bool IsMoveable(PathCell cell, int goalX, int goalY)
    {
        return IsWalkable(cell, goalX, goalY);
    }

    protected virtual bool IsBlockVision()
    {
        return false;
    }
    
    public bool BlockVision()
    {
        return IsBlockVision();
    }

    public void SetMoveable(bool moveable)
    {
        return;
    }

    // ── Events ──────────────────────────────────────────────────────────

    private UniTask OnMoveStart(AgentMover.MoveStartEvent args)
    {
        var startGrid = args.StartPosition.SnapToGrid();
        var targetGrid = args.TargetPosition.SnapToGrid();

        PathFinder.Instance.Move((int)startGrid.x, (int)startGrid.y, (int)targetGrid.x, (int)targetGrid.y, this);
        return UniTask.CompletedTask;
    }

    public virtual void InitAfterLevelLoad()
    {
        if (!_updateNavigation)
            return;
        this.Subscribe<AgentMover.MoveStartEvent>(OnMoveStart);
        
        var gridPosition = transform.position.SnapToGrid();
        transform.position = gridPosition.GridToWorld();

        X = (int)gridPosition.x;
        Y = (int)gridPosition.y;
        if (GridSizeX < 1) GridSizeX = 1;
        if (GridSizeZ < 1) GridSizeZ = 1;

        Layer = 1 << gameObject.layer;
        PathFinder.Instance.UpdateCell(X, Y, this);
    }
}
