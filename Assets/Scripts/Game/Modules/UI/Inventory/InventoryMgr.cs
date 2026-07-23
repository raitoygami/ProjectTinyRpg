using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GameState
{
    [Serializable]
    public class InventoryState
    {
        public Dictionary<long, ItemStack> ItemStacks = new();
    } 
    public InventoryState Inventory = new();

}

public class InventoryManager : Singleton<InventoryManager> // 使用 Odin 序列化字典
{
    // persist
    private GameState.InventoryState RuntimeData => Persist.Instance.GetState().Inventory;
    public IEnumerable<ItemStack> AllItems => RuntimeData.ItemStacks.Values;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public long[,] Occupied { get; private set; }

    public void Init(int width, int height)
    {
        Width = Mathf.Max(1, width);
        Height = Mathf.Max(1, height);
        Occupied = new long[Width, Height];
        RebuildOccupied();
    }

    /// <summary>根据 uidToItemMap 重建 occupied 数组（用于初始化或加载存档后）</summary>
    private void RebuildOccupied()
    {
        // 清空占用表
        for (var x = 0; x < Width; x++)
        for (var y = 0; y < Height; y++)
            Occupied[x, y] = 0;

        foreach (var item in RuntimeData.ItemStacks.Values)
        {
            if (item.IsEmpty) continue;
            // 只有坐标有效的物品才占用格子（装备等 PivotRow = -1 不占用）
            if (item.PivotCol >= 0 && item.PivotRow >= 0)
                TetrisMisc.FillRegion(Occupied, item.PivotCol, item.PivotRow, item.Width, item.Height, item.Uid);
        }
    }

    /// <summary>根据 Uid 获取物品</summary>
    public ItemStack GetItem(long uid)
    {
        return RuntimeData.ItemStacks.TryGetValue(uid, out var item) ? item : null;
    }

    public bool HasSameStack(ItemStack itemStack)
    {
        return itemStack.Stackable 
               && RuntimeData.ItemStacks.ContainsKey(itemStack.Uid) 
               && !RuntimeData.ItemStacks.ContainsValue(itemStack);
    }
    
    /// <summary>
    ///     拾取物品：清除占用但不从数据中移除。返回物品的拷贝或原对象（由调用方决定）。
    /// </summary>
    public ItemStack PickupItem(long uid)
    {
        if (!RuntimeData.ItemStacks.TryGetValue(uid, out var item))
            return null;

        // 清除占用
        TetrisMisc.ClearRegion(Occupied, item.PivotCol, item.PivotRow, item.Width, item.Height);
        // 注意：item 仍保留在 uidToItemMap 中，但占用已空，锚点仍为旧值（放下时会更新）
        return item;
    }

    /// <summary>
    ///     尝试放下手中物品到指定锚点，可能触发交换。
    /// </summary>
    /// <param name="holdItem">手中物品（不能为 null 或空）</param>
    /// <param name="targetPivotCol">目标锚点列</param>
    /// <param name="targetPivotRow">目标锚点行</param>
    /// <param name="swappedItem">若交换成功，返回被交换的物品（占用已清空，可供手中持有）</param>
    /// <returns>放置成功返回 true，否则 false</returns>
    public bool TryDropItem(ItemStack holdItem, int targetPivotCol, int targetPivotRow, out ItemStack swappedItem, out int pivotCol, out int pivotRow)
    {
        swappedItem = null;
        pivotCol = targetPivotCol;
        pivotRow = targetPivotRow;
        if (holdItem == null || holdItem.IsEmpty)
            return false;

        var width = holdItem.Width;
        var height = holdItem.Height;

        // 边界检查
        if (!TetrisMisc.IsRegionInBounds(Occupied, targetPivotCol, targetPivotRow, width, height))
            return false;

        // item stackable
        if (holdItem.Stackable && RuntimeData.ItemStacks.ContainsKey(holdItem.Uid))
        {
            RuntimeData.ItemStacks[holdItem.Uid].Count += holdItem.Count;
            return true;
        }
        
        // 获取目标区域占用情况
        var regionUid = TetrisMisc.GetRegionSingleUid(Occupied, targetPivotCol, targetPivotRow, width, height);
        if (regionUid == -1) // 多个不同物品混合，非法
            return false;

        if (regionUid == 0)
        {
            // 空白，直接放置
            PlaceItem(holdItem, targetPivotCol, targetPivotRow);
            if (!RuntimeData.ItemStacks.TryAdd(holdItem.Uid, holdItem))
                Debug.LogError($"Always has item {holdItem.Uid}");

            return true;
        }

        // 有且仅有一个其他物品
        if (!RuntimeData.ItemStacks.TryGetValue(regionUid, out var targetItem))
            return false;

        // 不能与自己交换（若手中物品 Uid 与目标相同，说明数据异常）
        if (targetItem.Uid == holdItem.Uid)
            return false;

        // 1. 清除目标物品占用
        TetrisMisc.ClearRegion(Occupied, targetItem.PivotCol, targetItem.PivotRow, targetItem.Width,
            targetItem.Height);

        // 2. 放置手中物品
        PlaceItem(holdItem, targetPivotCol, targetPivotRow);
        if (!RuntimeData.ItemStacks.TryAdd(holdItem.Uid, holdItem))
            Debug.LogError($"Always has item {holdItem.Uid}");

        Debug.Log($"Swapped Item{targetItem.Uid}");

        // 3. 返回被交换的物品
        swappedItem = targetItem;
        return true;
    }

    /// <summary>将物品放回原位（用于取消拖拽），原位必须为空</summary>
    public bool ReturnItemStackToOriginal(ItemStack item)
    {
        if (!RuntimeData.ItemStacks.ContainsKey(item.Uid))
            return false;

        var width = item.Width;
        var height = item.Height;
        var col = item.PivotCol;
        var row = item.PivotRow;

        if (!TetrisMisc.IsRegionInBounds(Occupied, col, row, width, height))
            return false;

        var regionUid = TetrisMisc.GetRegionSingleUid(Occupied, col, row, width, height);
        if (regionUid != 0)
            return false;

        PlaceItem(item, col, row);
        return true;
    }

    /// <summary>将物品放入背包并占用格子（用于新增物品）</summary>
    public bool AddItemStack(ItemStack newItem, int pivotCol, int pivotRow)
    {
        if (newItem == null || newItem.IsEmpty)
            return false;

        if (!TetrisMisc.IsRegionInBounds(Occupied, pivotCol, pivotRow, newItem.Width, newItem.Height))
            return false;

        var regionUid = TetrisMisc.GetRegionSingleUid(Occupied, pivotCol, pivotRow, newItem.Width, newItem.Height);
        if (regionUid != 0)
            return false;

        // 确保 Uid 唯一且已加入字典
        RuntimeData.ItemStacks[newItem.Uid] = newItem;
        PlaceItem(newItem, pivotCol, pivotRow);
        return true;
    }

    /// <summary>移除物品（从字典和占用表中彻底删除）</summary>
    public bool RemoveItemStack(long uid)
    {
        if (!RuntimeData.ItemStacks.TryGetValue(uid, out _))
            return false;

        RuntimeData.ItemStacks.Remove(uid);
        return true;
    }

    // ---------- 内部辅助方法 ----------
    private void PlaceItem(ItemStack item, int pivotCol, int pivotRow)
    {
        item.PivotCol = pivotCol;
        item.PivotRow = pivotRow;
        TetrisMisc.FillRegion(Occupied, pivotCol, pivotRow, item.Width, item.Height, item.Uid);
    }

    public bool TryAddItemStack(ItemStack item)
    {
        if (item == null || item.IsEmpty || item.Width <= 0 || item.Height <= 0)
        {
            Debug.LogWarning("TryAddItemStack: 无效的物品");
            return false;
        }

        if (item.Stackable && RuntimeData.ItemStacks.ContainsKey(item.Uid))
        {
            RuntimeData.ItemStacks[item.Uid].Count += item.Count;
            return true;
        }
        
        // 防止重复添加已存在的 Uid
        if (RuntimeData.ItemStacks.ContainsKey(item.Uid))
        {
            Debug.LogWarning($"TryAddItemStack: Uid {item.Uid} 已存在于背包中");
            return false;
        }

        var cellCount = new Vector2Int(item.Width, item.Height);
        if (!TetrisMisc.TryPlaceInOccupied(Occupied, item.Uid, cellCount, out var pivot))
        {
            Debug.Log("背包空间不足，无法放入物品");
            return false;
        }

        // 填充占用表
        TetrisMisc.FillRegion(Occupied, pivot.x, pivot.y, item.Width, item.Height, item.Uid);

        // 更新物品锚点并加入字典
        item.PivotCol = pivot.x;
        item.PivotRow = pivot.y;
        RuntimeData.ItemStacks[item.Uid] = item;

        return true;
    }

}