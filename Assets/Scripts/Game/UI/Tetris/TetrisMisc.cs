using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     拖拽放置预览状态。
/// </summary>
public enum TetrisDropPreviewState
{
    None, // 未拖拽或无效物品
    Valid, // 可放置（区域完全空白）
    Swap, // 可交换（区域有且仅有一个其他物品）
    Invalid // 不可放置（越界、多物品混合、或与自身重叠）
}

public static class TetrisMisc
{
    /// <summary>
    ///     在占用表中为指定 uid 寻找首个可放置位置（无旋转；锚点为左上格）。
    /// </summary>
    public static bool TryPlaceInOccupied(
        long[,] occupied,
        long uid,
        Vector2Int cellCount,
        out Vector2Int pivotColRow)
    {
        pivotColRow = default;
        if (occupied == null || uid == 0)
            return false;

        var sx = cellCount.x;
        var sy = cellCount.y;

        var aw = occupied.GetLength(0);
        var ah = occupied.GetLength(1);

        for (var pivotRow = 0; pivotRow <= ah - sy; pivotRow++) // 注意：原代码行列循环有误，修正
        for (var pivotCol = 0; pivotCol <= aw - sx; pivotCol++)
        {
            if (!RegionIsEmpty(occupied, pivotCol, pivotRow, sx, sy))
                continue;
            pivotColRow = new Vector2Int(pivotCol, pivotRow);
            return true;
        }

        return false;
    }

    private static bool RegionIsEmpty(long[,] occupied, int pivotCol, int pivotRow, int sx, int sy)
    {
        for (var dc = 0; dc < sx; dc++)
        for (var dr = 0; dr < sy; dr++)
            if (occupied[pivotCol + dc, pivotRow + dr] != 0)
                return false;

        return true;
    }

    /// <summary>
    ///     将指定矩形区域全部填充为 uid。
    /// </summary>
    public static void FillRegion(long[,] occupied, int pivotCol, int pivotRow, int sx, int sy, long uid)
    {
        for (var dc = 0; dc < sx; dc++)
        for (var dr = 0; dr < sy; dr++)
            occupied[pivotCol + dc, pivotRow + dr] = uid;
    }

    /// <summary>
    ///     清除指定矩形区域的占用（全部置为 0）。
    /// </summary>
    public static void ClearRegion(long[,] occupied, int pivotCol, int pivotRow, int sx, int sy)
    {
        FillRegion(occupied, pivotCol, pivotRow, sx, sy, 0);
    }

    /// <summary>
    ///     检查区域是否在网格边界内。
    /// </summary>
    public static bool IsRegionInBounds(long[,] occupied, int pivotCol, int pivotRow, int sx, int sy)
    {
        if (occupied == null) return false;
        var width = occupied.GetLength(0);
        var height = occupied.GetLength(1);
        return pivotCol >= 0 && pivotRow >= 0 &&
               pivotCol + sx <= width &&
               pivotRow + sy <= height;
    }

    /// <summary>
    ///     检测手中物品在指定锚点位置的放置预览状态。
    /// </summary>
    /// <param name="occupied">当前占用表</param>
    /// <param name="heldUid">手中物品的 Uid（若手中物品尚未从网格移除，会与自身冲突）</param>
    /// <param name="pivotCol">目标锚点列</param>
    /// <param name="pivotRow">目标锚点行</param>
    /// <param name="width">物品占用宽度（列数）</param>
    /// <param name="height">物品占用高度（行数）</param>
    /// <returns>预览状态枚举</returns>
    public static TetrisDropPreviewState GetDropPreviewState(
        long[,] occupied,
        long heldUid,
        int pivotCol, int pivotRow,
        int width, int height)
    {
        if (occupied == null || heldUid == 0 || width <= 0 || height <= 0)
            return TetrisDropPreviewState.None;

        if (!IsRegionInBounds(occupied, pivotCol, pivotRow, width, height))
            return TetrisDropPreviewState.Invalid;

        var regionUid = GetRegionSingleUid(occupied, pivotCol, pivotRow, width, height);

        if (regionUid == -1)
            return TetrisDropPreviewState.Invalid; // 多个不同物品混合

        if (regionUid == 0)
            return TetrisDropPreviewState.Valid; // 完全空白

        // 区域仅有一个物品
        if (regionUid == heldUid)
            return TetrisDropPreviewState.Invalid; // 不能与自己交换

        return TetrisDropPreviewState.Swap; // 可与其他单一物品交换
    }

    /// <summary>
    ///     获取矩形区域内物品占用情况。
    /// </summary>
    /// <returns>0：空白；正数：单一物品的 Uid；-1：存在多个不同 Uid</returns>
    public static long GetRegionSingleUid(long[,] occupied, int pivotCol, int pivotRow, int width, int height)
    {
        long foundUid = 0;
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
        {
            var uid = occupied[pivotCol + x, pivotRow + y];
            if (uid == 0) continue;

            if (foundUid == 0)
                foundUid = uid;
            else if (uid != foundUid) return -1;
        }

        return foundUid;
    }

    /// <summary>
    ///     获取物品在指定锚点下覆盖的所有格子坐标（列, 行）。
    /// </summary>
    public static List<Vector2Int> GetCoveredCells(int pivotCol, int pivotRow, int width, int height)
    {
        var cells = new List<Vector2Int>(width * height);
        for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
            cells.Add(new Vector2Int(pivotCol + x, pivotRow + y));
        return cells;
    }

    /// <summary>
    ///     便捷重载：直接从 ItemStack 获取尺寸并计算预览状态。
    /// </summary>
    public static TetrisDropPreviewState GetDropPreviewState(
        long[,] occupied,
        ItemStack heldItem,
        int pivotCol, int pivotRow)
    {
        if (heldItem == null || heldItem.IsEmpty)
            return TetrisDropPreviewState.None;
        return GetDropPreviewState(occupied, heldItem.Uid, pivotCol, pivotRow, heldItem.Width, heldItem.Height);
    }

    /// <summary>
    ///     根据鼠标在网格布局矩形内的本地坐标、网格参数及物品尺寸，计算物品应放置的锚点格子索引（物品中心对准鼠标）。
    /// </summary>
    /// <param name="localPoint">鼠标在 layoutRect 内的本地坐标</param>
    /// <param name="gridWidthCells">网格总列数</param>
    /// <param name="gridHeightCells">网格总行数</param>
    /// <param name="cellSize">单个格子尺寸（像素）</param>
    /// <param name="itemWidthCells">物品占用的列数</param>
    /// <param name="itemHeightCells">物品占用的行数</param>
    /// <param name="pivotCol">输出的锚点列索引</param>
    /// <param name="pivotRow">输出的锚点行索引</param>
    public static void CalculateGridPivot(
        Vector2 localPoint,
        int gridWidthCells,
        int gridHeightCells,
        Vector2 cellSize,
        int itemWidthCells,
        int itemHeightCells,
        out int pivotCol,
        out int pivotRow)
    {
        var itemWidth = itemWidthCells * cellSize.x;
        var itemHeight = itemHeightCells * cellSize.y;

        // 期望的物品左上角坐标（物品中心跟随鼠标）
        var desiredLeft = localPoint.x - itemWidth * 0.5f;
        var desiredTop = localPoint.y + itemHeight * 0.5f; // UI坐标系Y向下为正

        pivotCol = Mathf.RoundToInt(desiredLeft / cellSize.x);
        pivotRow = Mathf.RoundToInt(-desiredTop / cellSize.y); // 行索引从上到下增大，而坐标Y向下为负

        /*// 钳位到有效范围
        pivotCol = Mathf.Clamp(pivotCol, 0, gridWidthCells - itemWidthCells);
        pivotRow = Mathf.Clamp(pivotRow, 0, gridHeightCells - itemHeightCells);*/
    }
    
}