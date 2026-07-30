using UnityEngine;
using UnityEngine.EventSystems;
// item icon （A）装备流程
// 0. 先找到槽位 slot
// 1. 把A 从IItemIconOwner 移除 RemoveFromOwner->TryRemove
// 2. 然后尝试将A放入slot IItemIconOwner.TryAdd
// 3. 如果失败了则调用Restore
public interface IItemIconOwner : IEventSystemHandler
{
    
    public bool TryAdd(ItemIconObj itemIconObj, int location);
    
    public bool TryRemove(ItemIconObj itemIconObj);
    
    /// 丢弃物品， 将物品拖拽道空白位置（eventData.pointerCurrentRaycast.gameObject == null）的时候
    public void Discard(ItemIconObj itemIconObj);
    
    public void Restore(ItemIconObj itemIconObj);
    
    public void OnDrop(PointerEventData eventData, ItemIconObj itemIconObj);
    
}
