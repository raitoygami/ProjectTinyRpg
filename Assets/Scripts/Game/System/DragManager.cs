using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragManager : Singleton<DragManager>
{

    public class DragItemIconFinishEvt : EventArgs
    {
        
    }
    private DragItemIconFinishEvt  _dragItemIconFinishEvt = new();
    
    
    public void OnBeginDrag(PointerEventData eventData, ItemIconObj itemIconObj)
    {
        var layer = UIRoot.Instance.GetLayerCarry();
        itemIconObj.transform.SetParent(layer);
    }
    
    public void OnDrag(PointerEventData eventData, ItemIconObj itemIconObj)
    {
        // 1. 获取组件
        var iconRect = itemIconObj.GetComponent<RectTransform>();
        var parentRect = UIRoot.Instance.GetLayerCarry();
        if (parentRect == null) return;

        var uiCamera = UIRoot.Instance.GetUICamera();

        // 2. 将当前鼠标屏幕坐标转为父节点局部坐标
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, 
            eventData.position, 
            uiCamera, 
            out var localMousePos
        );

        // 3. 直接设置锚点位置 = 鼠标局部坐标 + 偏移量
        iconRect.anchoredPosition = localMousePos;
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
            
            this.PublishGlobal(_dragItemIconFinishEvt);
            if (handled)
                return;
        }

        // 3. 如果没有合适的 IItemIconOwner，或 handler 拒绝了，则丢弃或回到原位
        itemIconObj.Discard(); // 或回到原始父级        
    }
    
}