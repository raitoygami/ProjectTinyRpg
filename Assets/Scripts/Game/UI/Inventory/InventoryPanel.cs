using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryPanel : MonoBehaviour
{
    [Header("拖拽预览颜色")] 
    [SerializeField] private Color validPreviewColor = new(0f, 1f, 0f, 0.5f); // 半透明绿色
    [SerializeField] private Color swapPreviewColor = new(1f, 0.92f, 0.016f, 0.5f); // 半透明黄色
    [SerializeField] private Color invalidPreviewColor = new(1f, 0f, 0f, 0.5f); // 半透明红

    [SerializeField] private ItemIconObj _itemIconTemplate;
    [SerializeField] private List<Transform> _InventorySlots = new();
    private readonly Dictionary<int, ItemIconObj> itemNodeMap = new();
    
    [SerializeField] private ToolTipPanel _ToolTipPanel;
    
    private void Awake()
    {
        this.SubscribeGlobal<DropItem.PickupItemEvt>(OnPickupItem);
    }

    private UniTask OnPickupItem(DropItem.PickupItemEvt arg)
    {
        var itemStack = arg.ItemStack;
        if (!itemNodeMap.TryGetValue(itemStack.Location, out var iconObj))
        {
            iconObj = Instantiate(_itemIconTemplate, _InventorySlots[itemStack.Location]);
            iconObj.transform.localPosition = Vector3.zero;
            itemNodeMap.Add(itemStack.Location, iconObj);
        }
        iconObj.SetItemStack(itemStack);
        
        return UniTask.CompletedTask;
    }

    private void Start()
    {
        RefreshAllItems();
    }

    /// <summary>
    ///     从 InventoryManager 中读取所有物品并刷新 UI 显示。
    ///     注意：假设当前 tetrisRoot 下没有其他非物品子物体，或你将物品节点统一管理。
    /// </summary>
    private void RefreshAllItems()
    {
        // 从 InventoryManager 获取所有物品并创建节点
        var inventoryData = PlayerManager.Instance.GetInventoryData();
        if (inventoryData == null) return;

        foreach (var itemStack in inventoryData.InventoryItems)
        {
            if (itemStack.Location >= _InventorySlots.Count)
                break;

            if (!itemNodeMap.TryGetValue(itemStack.Location, out var iconObj))
            {
                iconObj = Instantiate(_itemIconTemplate, _InventorySlots[itemStack.Location]);
                iconObj.transform.localPosition = Vector3.zero;
                itemNodeMap.Add(itemStack.Location, iconObj);
            }
            iconObj.SetItemStack(itemStack);
        }
    }

    private void OnDestroy()
    {
        foreach (var (_,itemIconObj) in itemNodeMap)
        {
            Destroy(itemIconObj.gameObject);
        }
        itemNodeMap.Clear();
    }
   
}