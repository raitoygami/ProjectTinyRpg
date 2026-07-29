using UnityEngine;
using UnityEngine.EventSystems;

public interface IItemIconOwner : IEventSystemHandler
{
    
    public bool TryAdd(ItemIconObj itemIconObj, int location);
    
    public bool TryRemove(ItemIconObj itemIconObj);
    
    /// 丢弃物品， 将物品拖拽道空白位置（eventData.pointerCurrentRaycast.gameObject == null）的时候
    public void Discard(ItemIconObj itemIconObj);
    
    public void Restore(ItemIconObj itemIconObj);
    
    public void OnDrop(PointerEventData eventData, ItemIconObj itemIconObj);
    
}
