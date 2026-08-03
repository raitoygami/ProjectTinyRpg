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

    Vector2Int IPathNodeAgent.GridPosition
    {
        get => new Vector2Int(_gridX, _gridZ);
        set
        {
            _gridX = value.x;
            _gridZ = value.y;
        }
    }

    Vector2Int IPathNodeAgent.GridSize => new Vector2Int(GridSizeX, GridSizeZ);

    /// <summary>Grid column.</summary>
    public int X
    {
        get => _gridX;
        set => _gridX = value;
    }

    /// <summary>Grid row.</summary>
    public int Y
    {
        get => _gridZ;
        set => _gridZ = value;
    }

    private int _gridX;
    private int _gridZ;

    /// <summary>Footprint width in grid cells (≥1).</summary>
    public int GridSizeX { get; set; } = 1;

    /// <summary>Footprint height in grid cells (≥1).</summary>
    public int GridSizeZ { get; set; } = 1;

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
        return true;
    }

    public bool IsMoveabled(PathCell cell, int goalX, int goalY)
    {
        return IsWalkable(cell, goalX, goalY);
    }

    public void SetMoveable(bool moveable)
    {
        return;
    }

    // ── Events ──────────────────────────────────────────────────────────

    private UniTask OnMoveStart(AgentMover.MoveStartEvent args)
    {
        var sgrid = args.StartPosition.SnapToGrid();
        var tgrid = args.TargetPosition.SnapToGrid();

        PathFinder.Instance.Move((int)sgrid.x, (int)sgrid.y, (int)tgrid.x, (int)tgrid.y, this);
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
