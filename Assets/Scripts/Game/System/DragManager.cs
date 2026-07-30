using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragManager : Singleton<DragManager>
{

    public void OnBeginDrag(PointerEventData eventData, ItemIconObj itemIconObj)
    {
        var layer = UIRoot.Instance.GetLayerCarry();
        itemIconObj.transform.SetParent(layer);
    }
    
    public void OnDrag(PointerEventData eventData, ItemIconObj itemIconObj)
    {
        itemIconObj.GetComponent<RectTransform>().anchoredPosition += eventData.delta;
    }

    public void OnEndDrag(PointerEventData eventData, ItemIconObj itemIconObj)
    {
        // 1. 获取鼠标下的第一个对象（或使用 eventData.pointerCurrentRaycast）
        var target = eventData.pointerCurrentRaycast.gameObject;
        if (target == null)
        {
            itemIconObj.Discard();
            return;
        }
        
        // 2. 尝试在 target 及其父级中查找实现了 IDropHandler 的组件
        var dropHandler = ExecuteEvents.GetEventHandler<IItemIconOwner>(target);
        if (dropHandler != null)
        {
            var handled = ExecuteEvents.Execute<IItemIconOwner>(
                dropHandler, 
                eventData, 
                (handler, data) => handler.OnDrop((PointerEventData)data, itemIconObj)
            );

            // 重新绑定
            Context.Instance.PlayerInst.RebindWeapon().Forget();
            
            if (handled)
                return;
        }

        // 3. 如果没有合适的 IItemIconOwner，或 handler 拒绝了，则丢弃或回到原位
        itemIconObj.Discard(); // 或回到原始父级        
    }
    
}