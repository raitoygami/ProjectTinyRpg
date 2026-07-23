using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TetrisHandle : Singleton<TetrisHandle>
{
    private bool _isDragging;
    private Vector3 _lastMousePosition;
    private TetrisItemNode _draggedNode;
    [SerializeField] private Transform _dragRoot; // 拖拽时物品节点的临时父节点

    public bool IsDragging()
    {
        return _isDragging;
    }
    
    public class TetrisDragBeginEvent : EventArgs
    {
        public TetrisItemNode Node;
        
    }
    public class TetrisDragEvent : EventArgs
    {
        public Vector2 MousePosition;
        public TetrisItemNode Node;
    }
    public class TetrisDragEndEvent : EventArgs
    {
        public TetrisItemNode Node;
        public ITetrisItemSource Owner;
    }

    private readonly TetrisDragBeginEvent _tetrisDragBegin = new();
    private readonly TetrisDragEndEvent _tetrisDragEnd = new();
    private readonly TetrisDragEvent _tetrisDrag = new();
    
    public void OnNodeClicked(TetrisItemNode node, PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (!_isDragging)
                StartDragging(node);
            else
                TryTetrisPlaced();
        }else if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (_isDragging)
            {
                CancelDrag();
            }
            else
            {
                Debug.Log("根据node.source决定操作,包裹中的打开菜单");
            }
        }

    }

    
    
    private void StartDragging(TetrisItemNode node, bool swap = false)
    {
        if (node == null || node.ItemStack == null || node.Owner == null)
            return;

        _draggedNode = node;
        _tetrisDragBegin.Node = node;
        this.PublishGlobal(_tetrisDragBegin);
        if (!swap)
            node.Owner.PickupItem(node.ItemStack.Uid);

        // 将节点移至拖拽根节点下，并置于顶层
        if (_dragRoot != null) _draggedNode.transform.SetParent(_dragRoot);
        _isDragging = true;
        
        HandleTetrisDrag(true);
    }

    private void Update()
    {
        if (_isDragging && _draggedNode != null) HandleTetrisDrag();
    }

    private void HandleTetrisDrag(bool forced = false)
    {
        if (!forced && _lastMousePosition == Input.mousePosition)
            return;

        _lastMousePosition = Input.mousePosition;
        
        var rt = _draggedNode.GetComponent<RectTransform>();
        var parentRect = rt.parent as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRect,
                _lastMousePosition,
                UIRoot.Instance.GetUICamera(),
                out var localPoint))
            rt.anchoredPosition = localPoint;

        _tetrisDrag.Node = _draggedNode;
        _tetrisDrag.MousePosition = Input.mousePosition;
        this.PublishGlobal(_tetrisDrag);
    }

    private void TryTetrisPlaced()
    {
        if (!_isDragging || _draggedNode == null || _draggedNode.ItemStack == null) return;

        // 射线检测获取目标 ITetrisReceiver
        var eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        ITetrisItemSource targetReceiver = null;
        foreach (var result in results)
        {
            targetReceiver = result.gameObject.GetComponentInParent<ITetrisItemSource>();
            if (targetReceiver != null) break;
        }

        if (targetReceiver == null) return; // 无接收者，继续拖拽

        // 1. 检查目标是否可接收
        if (!targetReceiver.CanReceiveItem(_draggedNode.ItemStack, Input.mousePosition, out var placementContext))
            return;

        // 2. 从源移除数据（注意：源可能是目标自身，但操作顺序正确）
        var removed = _draggedNode.Owner.RemoveItem(_draggedNode);
        if (!removed)
        {
            Debug.LogError("从源移除物品失败");
            return;
        }

        _tetrisDragEnd.Owner = _draggedNode.Owner;
        _tetrisDragEnd.Node = _draggedNode;
        
        // 3. 在目标接收数据
        var received = targetReceiver.ReceiveItem(_draggedNode, placementContext, out var swappedItemNode);
        this.PublishGlobal(_tetrisDragEnd);
        
        if (received && swappedItemNode != null)
            StartDragging(swappedItemNode, true);
        else
            // 拖拽结束
            EndDragging();
    }

    private void EndDragging()
    {
        _draggedNode = null;
        _isDragging = false;
    }

    /// <summary>
    /// 取消拖拽，将物品节点还原到原始属主的容器中，并通知属主恢复占用状态。
    /// </summary>
    public void CancelDrag()
    {
        if (!_isDragging || _draggedNode == null || _draggedNode.Owner == null)
            return;

        var owner = _draggedNode.Owner;

        // 调用接口方法，让属主负责恢复占用和位置重置
        var returned = owner.ReturnItemToOriginalPosition(_draggedNode);

        if (!returned)
        {
            Debug.Log("复位失败");
            return;
        }

        // 发送拖拽结束事件
        _tetrisDragEnd.Node = _draggedNode;
        this.PublishGlobal(_tetrisDragEnd);

        // 清理拖拽状态
        EndDragging();
    }
}