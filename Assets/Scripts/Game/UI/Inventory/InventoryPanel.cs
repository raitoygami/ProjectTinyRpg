using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryPanel : MonoBehaviour, ITetrisItemSource, ITetrisLayoutOwner
{
    [Header("拖拽预览颜色")] 
    [SerializeField] private Color validPreviewColor = new(0f, 1f, 0f, 0.5f); // 半透明绿色
    [SerializeField] private Color swapPreviewColor = new(1f, 0.92f, 0.016f, 0.5f); // 半透明黄色
    [SerializeField] private Color invalidPreviewColor = new(1f, 0f, 0f, 0.5f); // 半透明红

    private class GridPlacementContext
    {
        public int PivotCol;
        public int PivotRow;
    }

    [SerializeField] private TetrisLayoutRenderer _tetrisLayoutRenderer;

    [SerializeField] private RectTransform tetrisRoot; // 格子父节点，也是物品父节点
    [SerializeField] private TetrisItemNode prefab;
    private readonly Dictionary<long, TetrisItemNode> itemNodeMap = new();
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }

    private void Awake()
    {
        Init(8, 10);
        this.SubscribeGlobal<TryAddItemEvt>(OnTryAddItem);
        this.SubscribeGlobal<TetrisHandle.TetrisDragEvent>(OnTetrisDrag);
        this.SubscribeGlobal<TetrisHandle.TetrisDragEndEvent>(OnTetrisDragEnd);
    }

    private void Start()
    {
        Canvas.ForceUpdateCanvases();
        // 加载并显示背包中已有的所有物品
        RefreshAllItems();
    }

    /// <summary>
    ///     从 InventoryManager 中读取所有物品并刷新 UI 显示。
    ///     注意：假设当前 tetrisRoot 下没有其他非物品子物体，或你将物品节点统一管理。
    /// </summary>
    private void RefreshAllItems()
    {
        foreach (var (_, node) in itemNodeMap) Destroy(node.gameObject);
        itemNodeMap.Clear();

        // 从 InventoryManager 获取所有物品并创建节点
        foreach (var item in InventoryManager.Instance.AllItems)
        {
            if (item == null || item.IsEmpty) continue;
            // 只处理有有效坐标的物品（装备等可能坐标为 -1）
            if (item.PivotCol >= 0 && item.PivotRow >= 0) CreateItemNode(item);
        }
    }

    /// <summary>
    ///     通过 ItemStack 获取对应的 TetrisItemNode（若存在）。
    /// </summary>
    public TetrisItemNode GetItemNode(ItemStack item)
    {
        if (item == null) return null;
        itemNodeMap.TryGetValue(item.Uid, out var node);
        Debug.Log($"{node != null}");
        return node;
    }

    /// <summary>
    ///     通过 Uid 获取对应的 TetrisItemNode。
    /// </summary>
    public TetrisItemNode GetItemNode(long uid)
    {
        itemNodeMap.TryGetValue(uid, out var node);
        return node;
    }

    private void CreateItemNode(ItemStack item)
    {
        if (prefab == null)
        {
            Debug.LogError("TetrisItemNode prefab is not assigned.");
            return;
        }

        // 实例化到 tetrisRoot 下
        var node = Instantiate(prefab, tetrisRoot);
        var nodeRect = node.GetComponent<RectTransform>();

        // 确保锚点和轴心为中心 (0.5, 0.5)，与预制体默认一致
        nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
        nodeRect.pivot = new Vector2(0.5f, 0.5f);

        // 先绑定数据以确定物品尺寸（不带动画）
        node.Bind(item, this);

        // 计算物品的实际像素尺寸
        var itemSize = CalculateTetrisSize(item);

        // 计算中心锚点下的 anchoredPosition
        var anchoredPos = CalculateAnchoredPosition(item.PivotCol, item.PivotRow, itemSize);
        nodeRect.anchoredPosition = anchoredPos;

        itemNodeMap[item.Uid] = node;
    }

    // 公开计算物品锚点位置的方法（供 TetrisHandle 取消拖拽时使用）
    public Vector2 CalculateAnchoredPositionForItem(ItemStack item)
    {
        var itemSize = CalculateTetrisSize(item);
        return CalculateAnchoredPosition(item.PivotCol, item.PivotRow, itemSize);
    }

    private Vector2 CalculateAnchoredPosition(int pivotCol, int pivotRow, Vector2 itemSize)
    {
        // 获取格子的四个角的世界坐标
        var corners = new Vector3[4];
        _tetrisLayoutRenderer.GetWorldCorners(pivotCol, pivotRow, corners);
        var worldTopLeft = corners[1]; // 左上角

        // 获取 UI 相机（Screen Space Camera 必须传入）
        var uiCamera = UIRoot.Instance.GetUICamera();

        // 世界坐标 → 屏幕坐标
        var screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldTopLeft);

        // 屏幕坐标 → tetrisRoot 本地坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            tetrisRoot,
            screenPoint,
            uiCamera,
            out var localTopLeft);

        // 计算物品中心在 tetrisRoot 中的本地坐标
        var centerX = localTopLeft.x + itemSize.x * 0.5f;
        var centerY = localTopLeft.y - itemSize.y * 0.5f; // UI 坐标系 Y 向下为正

        return new Vector2(centerX, centerY);
    }

    public void Init(int width, int height)
    {
        GridWidth = Mathf.Max(1, width);
        GridHeight = Mathf.Max(1, height);

        _tetrisLayoutRenderer.Init(GridWidth, GridHeight, this);
    }

    #region ITetrisItemSource

    public Vector2 CalculateTetrisSize(ItemStack itemStack)
    {
        var cellSize = _tetrisLayoutRenderer.CellSize;
        return new Vector2(cellSize.x * itemStack.Width, cellSize.y * itemStack.Height);
    }

    public bool ReturnItemToOriginalPosition(TetrisItemNode node)
    {
        if (node == null || node.ItemStack == null)
            return false;

        var item = node.ItemStack;
        if (!InventoryManager.Instance.ReturnItemStackToOriginal(item))
            return false;

        // 将节点父级设置回 tetrisRoot，并重新计算位置
        node.transform.SetParent(tetrisRoot);
        var nodeRect = node.GetComponent<RectTransform>();
        nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
        nodeRect.pivot = new Vector2(0.5f, 0.5f);

        var itemSize = CalculateTetrisSize(item);
        var anchoredPos = CalculateAnchoredPosition(item.PivotCol, item.PivotRow, itemSize);
        nodeRect.anchoredPosition = anchoredPos;

        return true;
    }

    public void PickupItem(long uid)
    {
        InventoryManager.Instance.PickupItem(uid);
    }

    public bool RemoveItem(TetrisItemNode itemNode)
    {
        var success = InventoryManager.Instance.RemoveItemStack(itemNode.ItemStack.Uid);
        if (success) itemNodeMap.Remove(itemNode.ItemStack.Uid);
        // 调用 InventoryManager 移除物品
        return success;
    }

    #endregion

    #region ITetrisReceiver

    public bool CanReceiveItem(ItemStack item, Vector2 screenPosition, out object placementContext)
    {
        placementContext = null;
        if (item == null || item.IsEmpty) return false;

        if (InventoryManager.Instance.HasSameStack(item))
        {
            var itemStack = InventoryManager.Instance.GetItem(item.Uid);
            placementContext = new GridPlacementContext {PivotCol = itemStack.PivotCol, PivotRow = itemStack.PivotRow};
            return true;
        }
        
        // 计算网格锚点
        if (!TryGetGridPosition(item, screenPosition, out var pivotCol, out var pivotRow))
            return false;

        // 检查数据层是否可放置
        var state = TetrisMisc.GetDropPreviewState(
            InventoryManager.Instance.Occupied,
            item,
            pivotCol, pivotRow);
        if (state != TetrisDropPreviewState.Valid && state != TetrisDropPreviewState.Swap)
            return false;

        placementContext = new GridPlacementContext {PivotCol = pivotCol, PivotRow = pivotRow};
        return true;
    }

    public bool ReceiveItem(TetrisItemNode itemNode, object placementContext, out TetrisItemNode swappedItemNode)
    {
        swappedItemNode = null;
        if (placementContext is not GridPlacementContext ctx)
            return false;

        var hasStackableItem = InventoryManager.Instance.HasSameStack(itemNode.ItemStack);
        
        // 执行数据层放置（可能交换）
        if (!InventoryManager.Instance.TryDropItem(itemNode.ItemStack, ctx.PivotCol, ctx.PivotRow, out var swappedItem , out var pivotCol, out var pivotRow))
            return false;

        if (hasStackableItem && itemNodeMap.TryGetValue(itemNode.ItemStack.Uid, out var item))
        {
            item.Refresh();
            Destroy(itemNode.gameObject);
            _tetrisLayoutRenderer.ClearPreview();
            return true;
        }
        
        // UI 更新：此时调用方会传入 TetrisItemNode 并处理父级/位置，此处只负责数据
        itemNode.SetOwer(this);
        itemNode.transform.SetParent(tetrisRoot);
        itemNodeMap.Add(itemNode.ItemStack.Uid, itemNode);

        var nodeRect = itemNode.GetComponent<RectTransform>();
        // 计算物品实际尺寸
        var itemSize = CalculateTetrisSize(itemNode.ItemStack);
        // 根据格子位置计算 anchoredPosition
        var anchoredPos = CalculateAnchoredPosition(pivotCol, pivotRow, itemSize);
        nodeRect.anchoredPosition = anchoredPos;

        if (swappedItem != null) swappedItemNode = GetItemNode(swappedItem);

        return true;
    }

    // 根据鼠标位置和物品尺寸计算应放置的网格锚点
    private bool TryGetGridPosition(ItemStack item, Vector2 screenPosition, out int pivotCol, out int pivotRow)
    {
        pivotCol = pivotRow = -1;

        var layoutRect = _tetrisLayoutRenderer.GetComponent<RectTransform>();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                layoutRect, screenPosition, UIRoot.Instance.GetUICamera(), out var localPoint))
            return false;
        // 补偿 layoutRect.pivot 带来的偏移，使得 localPoint 相对于网格左上角 (0,0) 格子的原点
        var cellSize = _tetrisLayoutRenderer.CellSize;

        var posFromTopLeft = localPoint + new Vector2(
            layoutRect.pivot.x * layoutRect.rect.width,
            (layoutRect.pivot.y - 1) * layoutRect.rect.height
        );
        // 调用静态方法计算锚点索引
        TetrisMisc.CalculateGridPivot(
            posFromTopLeft,
            GridWidth,
            GridHeight,
            cellSize,
            item.Width,
            item.Height,
            out pivotCol,
            out pivotRow);

        return true;
    }

    #endregion

    #region Pubsub

    public Color GetPreviewColor(TetrisDropPreviewState state)
    {
        return state switch
        {
            TetrisDropPreviewState.Valid => validPreviewColor,
            TetrisDropPreviewState.Swap => swapPreviewColor,
            TetrisDropPreviewState.Invalid => invalidPreviewColor,
            _ => Color.white // None 或未知状态返回透明
        };
    }

    private UniTask OnTryAddItem(TryAddItemEvt arg)
    {
        if (!arg.Success)
            return UniTask.CompletedTask;

        var stackItem = arg.Item;
        
        if (stackItem.Stackable && itemNodeMap.TryGetValue(stackItem.Uid, out var itemNode))
        {
            itemNode.Refresh();
            return UniTask.CompletedTask;    
        }
        
        CreateItemNode(stackItem);
        return UniTask.CompletedTask;
    }

    private int _pivotColLast = -1;
    private int _pivotRowLast = -1;

    private UniTask OnTetrisDrag(TetrisHandle.TetrisDragEvent arg)
    {
        if (TryGetGridPosition(arg.Node.ItemStack, arg.MousePosition, out var pivotCol, out var pivotRow))
        {
            if (_pivotColLast != pivotCol || _pivotRowLast != pivotRow)
            {
                _pivotColLast = pivotCol;
                _pivotRowLast = pivotRow;
                var state = TetrisMisc.GetDropPreviewState(
                    InventoryManager.Instance.Occupied,
                    arg.Node.ItemStack,
                    pivotCol, pivotRow);

                _tetrisLayoutRenderer.Preview(pivotCol, pivotRow,
                    arg.Node.ItemStack.Width,
                    arg.Node.ItemStack.Height,
                    GetPreviewColor(state)
                );
            }
        }
        else
        {
            _tetrisLayoutRenderer.ClearPreview();
        }

        return UniTask.CompletedTask;
    }

    private UniTask OnTetrisDragEnd(TetrisHandle.TetrisDragEndEvent arg)
    {
        if (arg.Owner == GetComponent<ITetrisItemSource>())
        {
            _tetrisLayoutRenderer.ClearPreview();
            _pivotColLast = -1;
            _pivotRowLast = -1;
        }

        return UniTask.CompletedTask;
    }

    #endregion


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!InventoryManager.HasInstance())
            return;
        var manager = InventoryManager.Instance;

        var occupied = manager.Occupied;
        if (occupied == null) return;

        var width = Mathf.Min(occupied.GetLength(0), GridWidth);
        var height = Mathf.Min(occupied.GetLength(1), GridHeight);

        for (var col = 0; col < width; col++)
        for (var row = 0; row < height; row++)
        {
            // 获取格子的四个角的世界坐标
            var corners = new Vector3[4];
            _tetrisLayoutRenderer.GetWorldCorners(col, row, corners);

            var isOccupied = occupied[col, row] != 0;

            // 1. 绘制半透明填充
            Handles.color = isOccupied
                ? new Color(1f, 0f, 0f, 0.3f) // 红色，透明度 0.3
                : new Color(0f, 1f, 0f, 0.3f); // 绿色，透明度 0.3
            Handles.DrawAAConvexPolygon(corners);

            // 2. 绘制边框
            Gizmos.color = isOccupied ? Color.red : Color.green;
            for (var i = 0; i < 4; i++)
            {
                var start = corners[i];
                var end = corners[(i + 1) % 4];
                Gizmos.DrawLine(start, end);
            }
        }
    }
#endif
}