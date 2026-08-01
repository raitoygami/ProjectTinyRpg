using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class EquipmentPanel : MonoBehaviour, IItemIconOwner
{
    [SerializeField] private ItemIconObj _itemIconTemplate;
    [SerializeField] private List<Transform> _EquipmentSlots = new();
    private readonly Dictionary<int, ItemIconObj> itemNodeMap = new();
    [SerializeField] private GameObject _equipActiveVfx;

    [SerializeField] private RectTransform _AvatarRoot;
    private RectTransform _AvatarMirrorInst;
    private void Awake()
    {
        this.SubscribeGlobal<Context.EquipmentUpdateEvt>(OnItemChanged);
        this.SubscribeGlobal<Context.AvatarChangedEvt>(OnAvatarChanged);
    }

    private UniTask OnAvatarChanged(Context.AvatarChangedEvt arg)
    {
        // 在这里更新
        RefreshAvatar();
        return UniTask.CompletedTask;
    }

    private UniTask OnItemChanged(Context.EquipmentUpdateEvt arg)
    {
        var location = PlayerManager.Instance.GetCurrWeaponLocation();
        _equipActiveVfx.gameObject.SetActive(location != -1);

        if (location == -1) return UniTask.CompletedTask;
        _equipActiveVfx.transform.SetParent(_EquipmentSlots[location]);
        _equipActiveVfx.transform.localPosition =  Vector3.zero;

        return UniTask.CompletedTask;
    }

    private void Start()
    {
        RefreshAllItems();
        RefreshAvatar();
    }

    private void RefreshAvatar()
    {
        // 在这里更新
        if (!Context.HasInstance() || Context.Instance.PlayerInst == null) return;
        if (_AvatarMirrorInst != null && _AvatarMirrorInst.gameObject != null)
        {
            Destroy(_AvatarMirrorInst.gameObject);
        }

        _AvatarMirrorInst = null;
            
        var avatarRoot = Context.Instance.PlayerInst.GetAvatarRoot();
        _AvatarMirrorInst = Utils.CreateUIMirror(avatarRoot.gameObject, _AvatarRoot);
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

        foreach (var itemStack in inventoryData.EquippedItems)
        {
            if (itemStack.Location >= _EquipmentSlots.Count)
                break;

            if (!itemNodeMap.TryGetValue(itemStack.Location, out var iconObj))
            {
                iconObj = Instantiate(_itemIconTemplate, _EquipmentSlots[itemStack.Location]);
                iconObj.transform.localPosition = Vector3.zero;
                itemNodeMap.Add(itemStack.Location, iconObj);
            }
            iconObj.SetOwner(this);
            iconObj.SetItemStack(itemStack);
        }
        
        var location = PlayerManager.Instance.GetCurrWeaponLocation();
        _equipActiveVfx.SetActive(location != -1);

        if (location != -1)
        {
            _equipActiveVfx.transform.SetParent(_EquipmentSlots[location]);
            _equipActiveVfx.transform.localPosition =  Vector3.zero;
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
        
        for (var i = 0; i < _EquipmentSlots.Count; i++)
        {
            if (_EquipmentSlots[i] == slotTransform || 
                slotTransform.IsChildOf(_EquipmentSlots[i]))
            {
                return i;
            }
        }

        return -1;
    }
    
    private void OnDestroy()
    {
        foreach (var (_,itemIconObj) in itemNodeMap)
        {
            Destroy(itemIconObj.gameObject);
        }
        itemNodeMap.Clear();

        if (_AvatarMirrorInst != null && _AvatarMirrorInst.gameObject != null)
        {
            Destroy(_AvatarMirrorInst.gameObject);
        }
        _AvatarMirrorInst = null;
        
    }

    #region IItemIconOwner

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
        var targetStack = inventoryData.EquippedItems.FirstOrDefault(i => i.Location == targetSlot);
        if (targetStack == null)
        {
            var result = PlayerManager.Instance.TryAddItemStackToEquipment(itemStack, targetSlot);
            if (result == PlayerManager.AddItemStackToEquipmentResult.SuccessEquipped)
            {
                itemIconObj.transform.SetParent(_EquipmentSlots[targetSlot]);
                itemIconObj.transform.localPosition = Vector3.zero;
                itemNodeMap.Add(targetSlot, itemIconObj);
                itemIconObj.SetOwner(this);
                return;
            }
        }
        else
        {
            var targetObj = itemNodeMap[targetSlot];
            if (itemOwner.TryAdd(targetObj, location))
            {
                var result = PlayerManager.Instance.TryAddItemStackToEquipment(itemStack, targetSlot);
                if (result == PlayerManager.AddItemStackToEquipmentResult.SuccessEquipped)
                {
                    itemIconObj.transform.SetParent(_EquipmentSlots[targetSlot]);
                    itemIconObj.transform.localPosition = Vector3.zero;
                    itemNodeMap.Add(targetSlot, itemIconObj);
                    itemIconObj.SetOwner(this);
                    return;
                }
            }
        }
        
        // 所有操作都失败了，则复位        
        itemIconObj.Restore();
    }

    public bool OnMouseRightClick(PointerEventData eventData, ItemIconObj itemIconObj)
    {
        if (!itemIconObj.GetOwner().Equals(this))
            return false;
        var itemStack = itemIconObj.GetItemStack();
        if (!itemStack.IsEquip())
            return false;
        var inventoryUI = UIRoot.Instance.InventoryUI;
        if (inventoryUI == null)
            return false;
        var inventoryPanel = inventoryUI.GetInventoryPanel();
        if (inventoryPanel == null)
            return false;
        var location = PlayerManager.Instance.GetFirstInventoryEmptySlot();
        
        return inventoryPanel.TryAdd(itemIconObj, location);
    }

    public void Add(ItemIconObj itemIconObj, int location)
    {
        var result  = PlayerManager.Instance.TryAddItemStackToEquipment(itemIconObj.GetItemStack(), location);
        if (result == PlayerManager.AddItemStackToEquipmentResult.SuccessEquipped)
        {
            itemNodeMap.Add(location, itemIconObj);
            itemIconObj.transform.SetParent(_EquipmentSlots[location]);
            itemIconObj.transform.localPosition = Vector3.zero;
            itemIconObj.SetOwner(this);
            return;
        }
        
        itemIconObj.Restore();
    }
    
    public bool TryAdd(ItemIconObj itemIconObj, int location)
    {
        if (!itemIconObj.RemoveFromOwner())
            return false;
        
        var result  = PlayerManager.Instance.TryAddItemStackToEquipment(itemIconObj.GetItemStack(), location);
        if (result == PlayerManager.AddItemStackToEquipmentResult.SuccessEquipped)
        {
            itemNodeMap.Add(location, itemIconObj);
            itemIconObj.transform.SetParent(_EquipmentSlots[location]);
            itemIconObj.transform.localPosition = Vector3.zero;
            itemIconObj.SetOwner(this);
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
        if (PlayerManager.Instance.RemoveItemStackFrontEquipment(itemIconObj.GetItemStack()))
        {
            itemNodeMap.Remove(itemIconObj.GetItemStack().Location);
            return true;
        }
        
        return false;
        
    }



    public ItemIconObj GetItemIconObj(int location)
    {
        return itemNodeMap.GetValueOrDefault(location);
    }
    
    public void Discard(ItemIconObj itemIconObj)
    {
        var itemStack = itemIconObj.GetItemStack();
        itemIconObj.transform.SetParent(_EquipmentSlots[itemStack.Location]);
        itemIconObj.transform.localPosition = Vector3.zero;
    }

    public void Restore(ItemIconObj itemIconObj)
    {
        var location = itemIconObj.GetItemStack().Location;
        var result = PlayerManager.Instance.TryAddItemStackToEquipment(itemIconObj.GetItemStack(), location);
        if (result == PlayerManager.AddItemStackToEquipmentResult.SuccessEquipped)
        {
            itemNodeMap.Add(location, itemIconObj);
            itemIconObj.transform.SetParent(_EquipmentSlots[location]);
            itemIconObj.transform.localPosition = Vector3.zero;
            itemIconObj.SetOwner(this);
        }
    }

    #endregion
}