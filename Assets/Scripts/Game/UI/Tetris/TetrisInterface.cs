using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ITetrisItemSource.cs
public interface ITetrisItemSource
{
    Vector2 CalculateTetrisSize(ItemStack itemStack);
    
    /// <summary>
    /// 拾取物品：从占用表中清除，但不从数据集合中移除。
    /// 返回拾取成功后的物品节点（可选），若失败返回 null。
    /// </summary>
    void PickupItem(long uid);
    
    /// <summary>
    /// 将物品放回原位（恢复占用），通常在取消拖拽时调用。
    /// </summary>
    bool ReturnItemToOriginalPosition(TetrisItemNode node);
    
    /// <summary>
    /// 从数据源中彻底移除物品（用于物品被放置到其他界面后）。
    /// </summary>
    bool RemoveItem(TetrisItemNode itemNode);
    
    /// <summary>
    /// 检查是否可以在屏幕位置接收物品，并返回放置所需的上下文（例如网格坐标或装备槽ID）。
    /// </summary>
    /// <param name="item">待放置物品</param>
    /// <param name="screenPosition">屏幕坐标</param>
    /// <param name="placementContext">放置上下文，用于后续 Receive 调用</param>
    /// <returns>是否可以接收</returns>
    bool CanReceiveItem(ItemStack item, Vector2 screenPosition, out object placementContext);

    /// <summary>
    /// 使用之前 CanReceiveItem 返回的上下文执行实际接收（数据层操作）。
    /// </summary>
    /// <param name="item">待放置物品</param>
    /// <param name="placementContext">由 CanReceiveItem 提供的上下文</param>
    /// <param name="swappedItem">若发生交换，返回被交换的物品</param>
    /// <returns>是否成功接收</returns>
    bool ReceiveItem(TetrisItemNode item, object placementContext, out TetrisItemNode swappedItemNode);
    
}

public interface ITetrisLayoutOwner
{
    
}

