using System.Collections.Generic;
using cfg;
using UnityEngine;

/// <summary>
/// 掉落面板：基于 <see cref="InventoryTetris"/> 的动态网格，实现 <see cref="IInventoryTetrisListener"/>。
/// 数据驱动：所有数据修改（从 <see cref="LootUnit.Stacks"/> 中增删改）先于视图变更。
/// 跨容器迁入/迁出由 <see cref="IItemStackTransferModule"/> 与拖拽解析器处理。
/// 每次道具转移或内部换位后同步 <see cref="DropSystem"/> 的持久化条目；LootUnit 清空后自动销毁。
/// </summary>
[Panel("Loot", "UI/LootUI", "HUDMid", EscBehavior =  EscBehavior.CloseOnly)]
public class LootUI : PanelBase
{
    /*[SerializeField] private InventoryTetris _inventory;

    private LootUnit _lootUnit;
    private ItemStack _carried;
    private bool _isOpen;
    private TetrisOccupancyGrid _occupancyGrid;

    public override bool IsOpen => _isOpen;
    public LootUnit CurrentLootUnit => _lootUnit;

    public ItemStack CarriedStack => _carried;
    public TetrisOccupancyGrid OccupancyGrid => _occupancyGrid;

    public bool TryGetStackForUid(long uid, out ItemStack stack)
    {
        stack = null;
        if (_lootUnit == null)
            return false;
        stack = _lootUnit.FindByUid(uid);
        if (stack != null && !stack.IsEmpty)
            return true;
        if (_carried != null && !_carried.IsEmpty && _carried.Uid == uid)
        {
            stack = _carried;
            return true;
        }
        return false;
    }

    public void CollectTetrisStacksForVisualSync(List<ItemStack> buffer)
    {
        buffer.Clear();
        if (_lootUnit == null)
            return;
        foreach (var s in _lootUnit.Stacks)
        {
            if (s == null || s.IsEmpty || s.Uid <= 0)
                continue;
            buffer.Add(s);
        }
    }

    public bool TryMergeCarriedOntoPlacedAtUid(long targetUid, out bool carryFullyConsumed)
    {
        carryFullyConsumed = false;
        return false;
    }

    public void RestoreCarriedEquipSlotCarry(InventoryTetris source) { }

    // ── ITetrisDragSource ────────────────────────────────────────────────

    IDragSessionHost ITetrisDragSource.Listener => this;
    RectTransform ITetrisDragSource.ItemsLayer => _inventory != null ? _inventory.ItemsLayer : null;
    Camera ITetrisDragSource.ResolveCameraForPlacement() => _inventory != null ? _inventory.ResolveCameraForPlacement() : null;
    RectTransform ITetrisDragSource.GetCarryOverlayRoot() => _inventory != null ? _inventory.GetCarryOverlayRoot() : null;
    void ITetrisDragSource.AttachCarryViewToOverlay(InventoryTetrisItem view) => _inventory?.AttachCarryViewToOverlay(view);
    void ITetrisDragSource.SyncCarryViewToMouse(InventoryTetrisItem view) => _inventory?.SyncCarryViewToMouse(view);

    // ── ITetrisItemViewPool ──────────────────────────────────────────────

    InventoryTetrisItem ITetrisItemViewPool.GetOrCreateItemView() => _inventory != null ? _inventory.GetOrCreateItemView() : null;
    void ITetrisItemViewPool.RecycleItemView(InventoryTetrisItem view) => _inventory?.RecycleItemView(view);
    void ITetrisItemViewPool.BindItemView(InventoryTetrisItem view, TetrisPlacedPiece piece, RectTransform layer) => _inventory?.BindItemView(view, piece, layer);
    void ITetrisItemViewPool.MatchItemViewSizeToGridCells(InventoryTetrisItem view, TetrisPlacedPiece piece) => _inventory?.MatchItemViewSizeToGridCells(view, piece);

    // ── Unity lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (_inventory == null)
            return;

        _inventory.RegisterListener(this);
        _occupancyGrid = new TetrisOccupancyGrid();
    }

    // ── Open / Close ────────────────────────────────────────────────────
    public void Open(LootUnit lootUnit)
    {
        if (lootUnit == null)
            return;

        _lootUnit = lootUnit;
        _carried = null;
        _isOpen = true;
        base.Open();

        RebuildGridFromLootUnit();
    }

    public override void Close()
    {
        if (TetrisItemDragSession.IsActive && TetrisItemDragSession.SourceGrid == _inventory)
            CancelCarryAndRestore();

        _inventory?.RebuildGrid(0);
        _occupancyGrid?.Init(1, 1);
        _carried = null;
        _lootUnit = null;
        _isOpen = false;
        base.Close();
    }

    public void RefreshFromLootUnit()
    {
        if (_lootUnit == null || _inventory == null)
            return;

        if (TetrisItemDragSession.IsActive && TetrisItemDragSession.SourceGrid == _inventory)
            CancelCarryAndRestore();

        RebuildGridFromLootUnit();
    }

    // ── Grid rebuild ────────────────────────────────────────────────────

    void RebuildGridFromLootUnit()
    {
        var columns = _inventory.Columns > 0 ? _inventory.Columns : 8;
        var slotCount = _lootUnit.CalculateRequiredSlotCount(columns);
        slotCount = Mathf.Max(slotCount, columns);

        _inventory.RebuildGrid(slotCount);
        _occupancyGrid.Init(_inventory.Columns, _inventory.Rows);

        var sizeX = GetComponent<RectTransform>().sizeDelta.x;
        GetComponent<RectTransform>().sizeDelta = new Vector2(sizeX, 130 + (float)slotCount / columns * 64);

        PopulateLootItems();
    }

    void PopulateLootItems()
    {
        if (_lootUnit == null || _inventory == null)
            return;

        if (!ConfigManager.HasInstance())
            return;

        foreach (var stack in _lootUnit.Stacks)
        {
            if (stack == null || stack.IsEmpty) continue;
            var def = ConfigManager.Instance.GetItemBase(stack.ItemId);
            if (def == null) continue;

            _inventory.TryAddItem(def, stack.Uid, stack.Count);
        }
    }

    // ── IInventoryTetrisListener: data callbacks ────────────────────────

    public void OnBeginCarry(long uid)
    {
        if (_lootUnit == null) return;
        _carried = _lootUnit.FindByUid(uid);
    }

    public void OnCancelCarry()
    {
        _carried = null;
    }

    public void OnDiscardCarry(long discardedPieceUid)
    {
        if (discardedPieceUid > 0 && _lootUnit != null)
            _lootUnit.RemoveByUid(discardedPieceUid);
        _carried = null;
        SyncDropEntries();
        TryClosePanelIfLootEmpty();
    }

    public void OnCommitOrRebuildFromVisual(int pivotCol, int pivotRow)
    {
        if (_carried != null)
        {
            _carried.PivotCol = pivotCol;
            _carried.PivotRow = pivotRow;
            _carried = null;
            SyncDropEntries();
            RebuildGridFromLootUnit();
            TryClosePanelIfLootEmpty();
            return;
        }

        RebuildLootDataFromVisual();
        SyncDropEntries();
        RebuildGridFromLootUnit();
        TryClosePanelIfLootEmpty();
    }

    public void OnCommitCarry(int pivotCol, int pivotRow)
    {
        if (_carried == null) return;
        _carried.PivotCol = pivotCol;
        _carried.PivotRow = pivotRow;
        _carried = null;
        SyncDropEntries();
    }

    public void OnRebuildAfterAutoSort()
    {
        if (TetrisItemDragSession.IsActive) return;
        RebuildLootDataFromVisual();
        SyncDropEntries();
    }

    // ── IItemStackTransferModule ────────────────────────────────────────

    public bool CanAcceptItemStack(ItemStack stack) =>
        stack != null && !stack.IsEmpty;

    public bool TryReceiveItemStackTransfer(ItemStack sourceStack) =>
        _lootUnit != null && _lootUnit.TryReceiveTransferredStack(sourceStack);

    public void CompleteItemStackTransferOut(ItemStack transferredStack = null)
    {
        long uid = 0;
        if (_carried != null && !_carried.IsEmpty)
            uid = _carried.Uid;
        else if (transferredStack != null && !transferredStack.IsEmpty)
            uid = transferredStack.Uid;
        else if (TetrisItemDragSession.IsActive && TetrisItemDragSession.Piece != null)
            uid = TetrisItemDragSession.Piece.Id;

        _carried = null;
        if (uid > 0 && _lootUnit != null)
            _lootUnit.RemoveByUid(uid);
        SyncDropEntries();
        TryClosePanelIfLootEmpty();
    }

    public void OnReceiveTransferInCompleted()
    {
        RefreshFromLootUnit();
        TryClosePanelIfLootEmpty();
    }

    // ── Discard from Loot -> World ──────────────────────────────────────

    public void OnDiscardedSpawnWorldLoot(TetrisPlacedPiece piece, InventoryTetris source)
    {
        if (piece?.Item == null) return;
        TrySpawnLootAtPlayerFeet(piece.Item.Id, Mathf.Max(1, piece.StackCount));
    }

    // ── Internal helpers ────────────────────────────────────────────────

    void RebuildLootDataFromVisual()
    {
        if (_lootUnit == null || _inventory == null || _occupancyGrid == null) return;
        _lootUnit.Stacks.Clear();
        foreach (var kv in _occupancyGrid.Pieces)
        {
            var p = kv.Value;
            if (p == null || p.Item == null) continue;
            _lootUnit.AddItemStack(new ItemStack
            {
                Uid = p.Id,
                ItemId = p.Item.Id,
                Count = Mathf.Max(1, p.StackCount),
                PivotCol = p.Pivot.x,
                PivotRow = p.Pivot.y
            });
        }
    }

    void CancelCarryAndRestore()
    {
        if (!TetrisItemDragSession.IsActive || TetrisItemDragSession.SourceGrid != _inventory)
            return;

        OnCancelCarry();

        var piece = TetrisItemDragSession.Piece;
        if (piece != null && piece.View != null)
            piece.View.SetCarryVisual(false);
        TetrisItemDragSession.Clear();
    }

    // ── Persistence sync ────────────────────────────────────────────────

    void SyncDropEntries()
    {
        if (_lootUnit == null) return;
        if (!DropSystem.HasInstance()) return;
        DropSystem.Instance.SyncDropEntriesFromLootUnit(_lootUnit);
    }

    void TryDestroyEmptyLootUnit()
    {
        if (_lootUnit == null || _lootUnit.Stacks.Count > 0) return;
        if (!DropSystem.HasInstance()) return;
        var lu = _lootUnit;
        _lootUnit = null;
        DropSystem.Instance.DestroyLootPile(lu);
    }

    void TryClosePanelIfLootEmpty()
    {
        if (_lootUnit != null)
        {
            if (_lootUnit.Stacks.Count > 0)
                return;
            TryDestroyEmptyLootUnit();
        }

        if (!IsOpen)
            return;
        if (UIRoot.HasInstance())
            UIRoot.Instance.CloseLootPanel();
        else
            Close();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    static void TrySpawnLootAtPlayerFeet(int itemId, int count)
    {
        if (itemId <= 0 || count <= 0) return;
        if (!DropSystem.HasInstance()) return;
        var player = GetPlayerEntity();
        if (player == null) return;
        DropSystem.Instance.DropItemStackFromEntity(player, itemId, count);
    }

    static Entity GetPlayerEntity()
    {
        if (!EntityManager.HasInstance())
            return null;
        var list = EntityManager.Instance.GetFractionEntities(EntityFaction.Player);
        return list != null && list.Count > 0 ? list[0] : null;
    }*/
}
