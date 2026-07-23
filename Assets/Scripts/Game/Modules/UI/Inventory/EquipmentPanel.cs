using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentPanel : MonoBehaviour, ITetrisItemSource
{
    [SerializeField] private TetrisItemNode prefab;
    [SerializeField] private Color validPreviewColor = new(0f, 1f, 0f, 0.5f); // 半透明绿色
    [SerializeField] private Color swapPreviewColor = new(1f, 0.92f, 0.016f, 0.5f); // 半透明黄色
    [SerializeField] private Color invalidPreviewColor = new(1f, 0f, 0f, 0.5f); // 半透明红

    [Serializable]
    public class SlotDef
    {
        public EquipType SlotType;
        public RectTransform Slots = new();
    }

    private class TetrisPlacementContext
    {
        public EquipType SlotType;
    }

    [SerializeField] private List<SlotDef> _Slots = new();

    private readonly Dictionary<long, TetrisItemNode> itemNodeMap = new();
    private Vector2 _cellSize = new(48, 48);

    private void Awake()
    {
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
        foreach (var item in EquipmentManager.Instance.AllItems)
        {
            // 实例化到 tetrisRoot 下
            var root = GetRoot(item);
            var node = Instantiate(prefab, root);
            var nodeRect = node.GetComponent<RectTransform>();
            nodeRect.anchoredPosition = Vector2.zero;

            // 确保锚点和轴心为中心 (0.5, 0.5)，与预制体默认一致
            nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRect.pivot = new Vector2(0.5f, 0.5f);

            // 先绑定数据以确定物品尺寸（不带动画）
            node.Bind(item, this);

            itemNodeMap[item.Uid] = node;
        }
    }

    private UniTask OnTetrisDragEnd(TetrisHandle.TetrisDragEndEvent arg)
    {
        foreach (var slotDef in _Slots) slotDef.Slots.GetComponent<Image>().color = Color.white;
        return UniTask.CompletedTask;
    }

    private UniTask OnTetrisDrag(TetrisHandle.TetrisDragEvent arg)
    {
        foreach (var slotDef in _Slots)
            if (RectTransformUtility.RectangleContainsScreenPoint(slotDef.Slots, arg.MousePosition,
                    UIRoot.Instance.GetUICamera()))
            {
                var equal = slotDef.SlotType == arg.Node.ItemStack.EquipType;
                if (equal)
                {
                    var itemNode = GetNode(slotDef.SlotType);
                    if (itemNode != null)
                        slotDef.Slots.GetComponent<Image>().color =
                            itemNode == arg.Node ? validPreviewColor : swapPreviewColor;
                    else
                        slotDef.Slots.GetComponent<Image>().color = validPreviewColor;
                }
                else
                {
                    slotDef.Slots.GetComponent<Image>().color = invalidPreviewColor;
                }
            }
            else
            {
                slotDef.Slots.GetComponent<Image>().color = Color.white;
            }

        return UniTask.CompletedTask;
    }

    public TetrisItemNode GetNode(EquipType slotType)
    {
        foreach (var (_, itemNode) in itemNodeMap)
        {
            if (itemNode.ItemStack.EquipType == slotType)
            {
                return itemNode;
            }
        }

        return null;
    }
    
    public Vector2 CalculateTetrisSize(ItemStack itemStack)
    {
        return new Vector2(_cellSize.x * itemStack.Width, _cellSize.y * itemStack.Height);
    }

    public void PickupItem(long uid)
    {
        EquipmentManager.Instance.PickUpItem(uid);
    }

    public bool ReturnItemToOriginalPosition(TetrisItemNode node)
    {
        if (!EquipmentManager.Instance.ReturnItemStackToOriginal(node.ItemStack))
            return false;

        node.transform.SetParent(GetRoot(node.ItemStack));
        var nodeRect = node.GetComponent<RectTransform>();
        nodeRect.anchoredPosition = Vector2.zero;
        
        return true;
    }

    public bool RemoveItem(TetrisItemNode itemNode)
    {
        var success = EquipmentManager.Instance.RemoveItemStack(itemNode.ItemStack);
        if (success)
        {
            itemNodeMap.Remove(itemNode.ItemStack.Uid);
        }

        return success;
    }

    public TetrisItemNode GetItemNode(ItemStack item)
    {
        if (item == null) return null;
        foreach (var (_, itemNode) in itemNodeMap)
        {
            if (itemNode.ItemStack.Uid == item.Uid)
            {
                return itemNode;
            }
        }
        return null;
    }

    private Transform GetRoot(ItemStack itemStack)
    {
        return (from slotDef in _Slots
            where slotDef.SlotType == itemStack.EquipType
            select slotDef.Slots.GetComponent<Transform>()).FirstOrDefault();
    }

    public bool CanReceiveItem(ItemStack item, Vector2 screenPosition, out object placementContext)
    {
        placementContext = null;
        foreach (var slotDef in _Slots.Where(slotDef => RectTransformUtility.RectangleContainsScreenPoint(slotDef.Slots,
                     screenPosition,
                     UIRoot.Instance.GetUICamera())))
        {
            if (slotDef.SlotType != item.EquipType) return false;
            placementContext = new TetrisPlacementContext() {SlotType = item.EquipType};
            return true;
        }

        return false;
    }

    public bool ReceiveItem(TetrisItemNode itemNode, object placementContext, out TetrisItemNode swappedItemNode)
    {
        swappedItemNode = null;
        if (placementContext is not TetrisPlacementContext ctx)
            return false;

        if (!EquipmentManager.Instance.TryDropItem(itemNode.ItemStack, out var swappedItem))
            return false;

        itemNode.SetOwer(this);
        itemNode.transform.SetParent(GetRoot(itemNode.ItemStack));
        itemNodeMap.Add(itemNode.ItemStack.Uid, itemNode);
        var nodeRect = itemNode.GetComponent<RectTransform>();
        nodeRect.anchoredPosition = Vector2.zero;
        
        swappedItemNode = GetItemNode(swappedItem);

        return true;
    }
}