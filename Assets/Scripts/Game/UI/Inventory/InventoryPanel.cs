using System.Collections.Generic;
using System.Linq;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;


public class InventoryPanel : MonoBehaviour, IItemIconOwner
{
    [SerializeField] private ItemIconObj _itemIconTemplate;
    [SerializeField] private List<Transform> _InventorySlots = new();
    private readonly Dictionary<int, ItemIconObj> itemNodeMap = new();

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
            iconObj.SetOwner(this);
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
                iconObj.SetOwner(this);
                iconObj.transform.localPosition = Vector3.zero;
                itemNodeMap.Add(itemStack.Location, iconObj);
            }

            iconObj.SetItemStack(itemStack);
        }
    }


    /// <summary>
    /// 根据 Drop 位置找到对应的槽位索引
    /// </summary>
    private int GetSlotIndexFromDrop(PointerEventData eventData)
    {
        if (eventData.pointerCurrentRaycast.gameObject == null)
            return -1;

        // 找到被 Drop 的槽位父物体
        var slotTransform = eventData.pointerCurrentRaycast.gameObject.transform;

        for (var i = 0; i < _InventorySlots.Count; i++)
        {
            if (_InventorySlots[i] == slotTransform ||
                slotTransform.IsChildOf(_InventorySlots[i]))
            {
                return i;
            }
        }

        return -1;
    }


    private void OnDestroy()
    {
        foreach (var (_, itemIconObj) in itemNodeMap)
        {
            Destroy(itemIconObj.gameObject);
        }

        itemNodeMap.Clear();
    }


    public void OnDrop(PointerEventData eventData, ItemIconObj itemIconObj)
    {
        // 获取目标槽位索引
        int targetSlot = GetSlotIndexFromDrop(eventData);
        if (targetSlot < 0)
        {
            itemIconObj.Discard();
            return;
        }

        // 原本的位置
        var location = itemIconObj.GetItemStack().Location;
        var itemOwner = itemIconObj.GetOwner();
        // 先从源头移除 item icon obj， 但是依旧引用着源头,和原本的位置
        if (!itemIconObj.RemoveFromOwner())
        {
            itemIconObj.Discard();
            return;
        }

        // 这时候已经成功将道具从源头移除了
        // 如果找到目标槽位, 则看目标位置有没有道具
        // 1:如果目标位置有道具

        var itemStack = itemIconObj.GetItemStack();

        var inventoryData = PlayerManager.Instance.GetInventoryData();
        // 查找目标位置是否已有物品
        // 即使放到原位置， 因为在之前已经从背包里将itemIconObj清除了，所以这里通过Location是找不到itemIconObj的
        var targetStack = inventoryData.InventoryItems.FirstOrDefault(i => i.Location == targetSlot);
        // 目标位置为空，则接受道具
        if (targetStack == null)
        {
            // 这个是必须成功的
            var result = PlayerManager.Instance.TryAddItemStackToInventory(itemStack, targetSlot);
            // 加入到当前slot上
            if (result == PlayerManager.AddItemStackToInventoryResult.SuccessNewInstance)
            {
                itemIconObj.transform.SetParent(_InventorySlots[targetSlot]);
                itemIconObj.transform.localPosition = Vector3.zero;
                itemNodeMap.Add(targetSlot, itemIconObj);
                itemIconObj.SetOwner(this);
                return;
            }
        }
        else
        {
            var targetObj = itemNodeMap[targetSlot];
            // 如果当前道具可以堆叠,且是相同道具, 则更新数量，同时销毁 item icon obj
            if (targetStack.StackEquals(itemIconObj.GetItemStack()) && targetStack.Stackable())
            {
                var result = PlayerManager.Instance.TryAddItemStackToInventory(itemStack, targetSlot);
                if (result == PlayerManager.AddItemStackToInventoryResult.SuccessStacked)
                {
                    Destroy(itemIconObj.gameObject);
                    targetObj.SetItemStack(itemStack);
                    return;
                }
            }

            // 如果成功交换
            if (itemOwner.TryAdd(targetObj, location))
            {
                var result = PlayerManager.Instance.TryAddItemStackToInventory(itemStack, targetSlot);
                // 加入到当前slot上
                if (result == PlayerManager.AddItemStackToInventoryResult.SuccessNewInstance)
                {
                    itemIconObj.transform.SetParent(_InventorySlots[targetSlot]);
                    itemIconObj.transform.localPosition = Vector3.zero;
                    itemNodeMap.Add(targetSlot, itemIconObj);
                    itemIconObj.SetOwner(this);
                    return;
                }
            }

            // 否则尝试将 targetStack和itemIconObj交换位置
        }

        // 所有操作都失败了，则复位        
        itemIconObj.Restore();
    }

    public bool OnMouseRightClick(PointerEventData eventData, ItemIconObj itemIconObj)
    {
        if (!itemIconObj.GetOwner().Equals(this))
            return false;

        var itemStack = itemIconObj.GetItemStack();
        var originLocation = itemStack.Location;
        if (itemStack.IsEquip())
        {
            var equipType = itemStack.GetEquipType();
            //  武器右键只能放到空位置上，因为武器没有固定槽位，在4-7号都可以放, 如果想放到指定位置，只能拖拽
            var inventoryUI = UIRoot.Instance.InventoryUI;
            if (inventoryUI == null)
                return false;

            var equipmentPanel = inventoryUI.GetEquipmentPanel();
            if (equipType == EquipType.Weapon)
            {
                var location = PlayerManager.Instance.GetFirstEmptyWeaponLocation(equipType);
                if (location == -1)
                    return false;

                if (!equipmentPanel.TryAdd(itemIconObj, location))
                {
                    itemIconObj.Restore();
                    return false;
                }
            }
            // 如果是防具, 不需要为空， 直接交换
            else
            {
                var location = PlayerManager.Instance.GetArmorLocation(equipType);
                if (location == -1)
                    return false;
                
                var inventoryData = PlayerManager.Instance.GetInventoryData();
                // 查找目标位置是否已有物品
                // 装备槽上是否有防具
                var targetStack = inventoryData.EquippedItems.FirstOrDefault(i => i.Location == location);
                // 没有则直接放上去
                if (targetStack == null)
                {
                    return equipmentPanel.TryAdd(itemIconObj, location);
                }
                
                //  如果有, 则先将targetStack从目标移除
                var equipArmor = equipmentPanel.GetItemIconObj(location);
                if (!equipmentPanel.TryRemove(equipArmor))
                    return false;
                
                if (!equipmentPanel.TryAdd(itemIconObj, location))
                {
                    itemIconObj.Restore();
                    equipArmor.Restore();
                    return false;
                }
                TryAdd(equipArmor, originLocation);
            }

            return true;
        }
        // 如果不是装备, 就要判断是不是消耗品
        else
        {
            
        }
        
        return false;
    }


    public bool TryAdd(ItemIconObj itemIconObj, int location)
    {
        // 移除
        if (!itemIconObj.RemoveFromOwner())
            return false;

        var result = PlayerManager.Instance.TryAddItemStackToInventory(itemIconObj.GetItemStack(), location);

        if (result == PlayerManager.AddItemStackToInventoryResult.SuccessNewInstance)
        {
            itemNodeMap.Add(location, itemIconObj);
            itemIconObj.transform.SetParent(_InventorySlots[location]);
            itemIconObj.transform.localPosition = Vector3.zero;
            itemIconObj.SetOwner(this);
            return true;
        }

        if (result == PlayerManager.AddItemStackToInventoryResult.SuccessStacked)
        {
            Destroy(itemIconObj.gameObject);
            itemNodeMap[location].SetItemStack(itemNodeMap[location].GetItemStack());
            return true;
        }

        itemIconObj.Restore();

        return false;
    }

    public bool TryRemove(ItemIconObj itemIconObj)
    {
        if (!itemIconObj.GetOwner().Equals(this))
            return false;

        // 如果 成功从当前包裹移除item
        if (PlayerManager.Instance.RemoveItemStackFrontInventory(itemIconObj.GetItemStack()))
        {
            itemNodeMap.Remove(itemIconObj.GetItemStack().Location);
            return true;
        }

        return false;
    }

    // 复位
    public void Restore(ItemIconObj itemIconObj)
    {
        var location = itemIconObj.GetItemStack().Location;
        var result = PlayerManager.Instance.TryAddItemStackToInventory(itemIconObj.GetItemStack(), location);
        if (result == PlayerManager.AddItemStackToInventoryResult.SuccessNewInstance)
        {
            itemNodeMap.Add(location, itemIconObj);
            itemIconObj.transform.SetParent(_InventorySlots[location]);
            itemIconObj.transform.localPosition = Vector3.zero;
            itemIconObj.SetOwner(this);
        }
        else if (result == PlayerManager.AddItemStackToInventoryResult.SuccessStacked)
        {
            // 可能是堆叠拿出一定数量仓库然后取消了
        }
    }

    public void Discard(ItemIconObj itemIconObj)
    {
        var itemStack = itemIconObj.GetItemStack();
        itemIconObj.transform.SetParent(_InventorySlots[itemStack.Location]);
        itemIconObj.transform.localPosition = Vector3.zero;
    }
}