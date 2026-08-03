using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using cfg;
using UnityEngine;

public partial class SaveData
{
    // 装备在包裹中就存放在InventoryItems
    // 装备已经在装备槽上就存放在EquippedItems
    [Serializable]
    public class ItemContainer
    {
        public Dictionary<long, ItemStack> LookupTable = new();
        // 已经装备的
        public List<long> Equipped = new();
        // 包裹中的
        public List<long> Inventory = new();
        public int ActiveWeaponSlotIndex = -1;

        public void AddItemStack(ItemStack itemStack)
        {
            LookupTable.Add(itemStack.Uid, itemStack);
        }

        public bool RemoveItemStack(ItemStack itemStack)
        {
            return LookupTable.Remove(itemStack.Uid);
        }
        
    }

    public ItemContainer Inventory = new();
}

public partial class PlayerManager
{
    public SaveData.ItemContainer GetSavedItemContainer()
    {
        return Persist.Instance.GetState().Inventory;
    }

    public void NewItemStack(ItemStack itemStack)
    {
        var container = GetSavedItemContainer();
        container.AddItemStack(itemStack);
    }

    public bool RemoveItemStack(ItemStack itemStack)
    {
        var container = GetSavedItemContainer();
        return container.RemoveItemStack(itemStack);
    }
    
    public ItemStack GetItemStackByUID(long uid)
    {
        var itemContainer = GetSavedItemContainer();
        return itemContainer.LookupTable.GetValueOrDefault(uid);
    }
    
    // 列
    public const int InventorySizeCol = 7;

    // 行
    public const int InventorySizeRow = 7;

    // 武器槽位就8个 固定
    private long[] OccupiedEquipped;

    // 背包槽位49个，暂定
    private long[] OccupiedInventory;
    
    // 装备增删改事件：参数 (槽位索引, 旧装备ItemStack, 新装备ItemStack)
    public event Action<int, ItemStack, ItemStack> OnEquipmentChanged;

    private void RebuildInventory()
    {
        OccupiedInventory = new long[InventorySizeRow * InventorySizeCol];
        for (var i = 0; i < InventorySizeRow * InventorySizeCol; i++)
        {
            OccupiedInventory[i] = 0;
        }

        OccupiedEquipped = new long[8];
        for (var i = 0; i < 8; i++)
        {
            OccupiedEquipped[i] = 0;
        }

        foreach (var uid in GetSavedItemContainer().Inventory)
        {
            var itemStack = GetItemStackByUID(uid);
            if (itemStack == null)
                continue;
            
            if (itemStack.Location < OccupiedInventory.Length)
                OccupiedInventory[itemStack.Location] = itemStack.Uid;
        }

        foreach (var uid in GetSavedItemContainer().Equipped)
        {
            var itemStack = GetItemStackByUID(uid);
            if (itemStack == null)
                continue;
            
            if (itemStack.Location < OccupiedEquipped.Length)
                OccupiedEquipped[itemStack.Location] = itemStack.Uid;
        }
    }

    #region Inventory

    public enum AddItemStackToInventoryResult
    {
        /// <summary>
        /// 操作失败（未知原因）
        /// </summary>
        Failure = 0,

        /// <summary>
        /// 成功新增物品（非堆叠）
        /// </summary>
        SuccessNewInstance = 1,

        /// <summary>
        /// 成功堆叠到已有物品上
        /// </summary>
        SuccessStacked = 2,

        /// <summary>
        /// 失败：背包已满，无法再添加新物品
        /// </summary>
        InventoryFull = 3,

        /// <summary>
        /// 失败：传入的参数无效（如 ItemStack 为空、数量≤0 等）
        /// </summary>
        InvalidArgument = 4,

        /// <summary>
        /// 失败：目标位置不为空
        /// </summary>
        NotEmptySlot = 5,

        /// <summary>
        /// 失败：数据同步出错 ？？？？？？
        /// </summary>
        InvalidData = 6,
    }
    // inventory part

    public AddItemStackToInventoryResult TryAddItemStackToInventory(long itemStackUID, int location)
    {
        if (location >= OccupiedInventory.Length)
            return AddItemStackToInventoryResult.InvalidArgument;

        var itemStack = GetItemStackByUID(itemStackUID);
        
        var uid = OccupiedInventory[location];
        // 如果当前位置为空,则直接加入背包
        if (uid == 0)
        {
            GetSavedItemContainer().Inventory.Add(itemStackUID);
            OccupiedInventory[location] = itemStack.Uid;
            itemStack.Location = location;
            return AddItemStackToInventoryResult.SuccessNewInstance;
        }
        // 当前位置非空

        if (!itemStack.Stackable())
            return AddItemStackToInventoryResult.NotEmptySlot;

        var existingItem = GetItemStackByUID(uid);
                
        if (existingItem == null)
            return AddItemStackToInventoryResult.InvalidData;

        if (existingItem.ItemId != itemStack.ItemId)
            return AddItemStackToInventoryResult.Failure;
        
        existingItem.Count += itemStack.Count;
        return AddItemStackToInventoryResult.SuccessStacked;
    }

    public AddItemStackToInventoryResult TryAddItemStackToInventory(long itemStackUID)
    {
        
        var itemStack = GetItemStackByUID(itemStackUID);
        if (itemStack == null)
            return AddItemStackToInventoryResult.Failure;
        
        //  如何可以堆叠，且包裹里有对应物体，则堆叠
        if (itemStack.Stackable())
        {
            foreach (var uid in  GetSavedItemContainer().Inventory)
            {
                var existingItem = GetItemStackByUID(uid);
                if (existingItem == null || existingItem.ItemId != itemStack.ItemId) continue;
                existingItem.Count += itemStack.Count;
                break;
            }
            return AddItemStackToInventoryResult.SuccessStacked;
        }

        // 如果没有，则找空位看看能不能加进去
        var emptySlot = GetFirstInventoryEmptySlot();
        if (emptySlot == -1) return AddItemStackToInventoryResult.Failure;

        GetSavedItemContainer().Inventory.Add(itemStackUID);
        OccupiedInventory[emptySlot] = itemStackUID;

        return AddItemStackToInventoryResult.SuccessNewInstance;
    }


    public AddItemStackToInventoryResult TryAddItemStackToInventory(int itemID, int amount, out ItemStack itemStack)
    {
        itemStack = null;

        var config = ConfigManager.Instance.GetItemBase(itemID);
        if (config == null)
        {
            Debug.LogError($"[AddItem] failed: not found item {itemID}");
            return AddItemStackToInventoryResult.Failure;
        }

        switch (config.Stackable)
        {
            case false when amount > 1:
                return AddItemStackToInventoryResult.InvalidArgument;
            case true:
            {
                foreach (var uid in GetSavedItemContainer().Inventory)
                {
                    var existingItem = GetItemStackByUID(uid);
                    if (existingItem != null && existingItem.ItemId != itemID)
                    {
                        existingItem.Count += amount; // 增加数量
                        itemStack = existingItem;
                        return AddItemStackToInventoryResult.SuccessStacked;
                    }
                }

                break;
            }
        }

        var emptySlot = GetFirstInventoryEmptySlot();
        if (emptySlot == -1)
            return AddItemStackToInventoryResult.InventoryFull;

        itemStack = new ItemStack
        {
            Uid = UidGenerator.Generate(itemID),
            ItemId = itemID,
            Count = amount,
            Location = emptySlot,
        };

        var itemContainer = GetSavedItemContainer();
        itemContainer.AddItemStack(itemStack);
        itemContainer.Inventory.Add(itemStack.Uid);
        OccupiedInventory[emptySlot] = itemStack.Uid;
        return AddItemStackToInventoryResult.SuccessNewInstance;
    }

    // 只从包裹中移除，不会更新 item stack 数据
    public bool RemoveItemStackFromInventory(long itemStackUID)
    {
        var itemStack = GetItemStackByUID(itemStackUID);
        if (GetSavedItemContainer().Inventory.Remove(itemStackUID))
        {
            OccupiedInventory[itemStack.Location] = 0;
            return true;
        }

        return false;
    }

    public int GetFirstInventoryEmptySlot()
    {
        for (var i = 0; i < OccupiedInventory.Length; i++)
        {
            if (OccupiedInventory[i] == 0)
                return i;
        }

        return -1;
    }

    #endregion

    #region Equipment

// 获取当前装备的Weapon
    public ItemStack GetCurrentWeapon()
    {
        RefreshWeaponActive();
        var itemContainer = GetSavedItemContainer(); 
        var index = itemContainer.ActiveWeaponSlotIndex;

        foreach (var uid in itemContainer.Equipped)
        {
            var itemStack = GetItemStackByUID(uid);
            if (itemStack != null && itemStack.Location == index)
                return itemStack;
        }

        return null;
    }

    private void SetCurrentWeaponLocation(int newLocation)
    {
        var oldLocation = GetSavedItemContainer().ActiveWeaponSlotIndex;
        if (oldLocation == newLocation) return;
        GetSavedItemContainer().ActiveWeaponSlotIndex = newLocation;
    }
    
    public int GetCurrWeaponLocation()
    {
        RefreshWeaponActive();
        return GetSavedItemContainer().ActiveWeaponSlotIndex;
    }

    public int GetNextWeaponLocation()
    {
        var location = GetSavedItemContainer().ActiveWeaponSlotIndex;
        // 若当前没有武器，说明一定没有装备武器
        if (location == -1)
            return -1;
        // 当前有武器，从下一个槽位开始循环查找
        // 总共4个武器槽，最多检查4次（包含自己一次，若只有自己则返回-1）
        for (var step = 1; step <= 4; step++)
        {
            var nextIdx = (location - 4 + step) % 4 + 4; // 循环移位
            if (OccupiedEquipped[nextIdx] != 0)
            {
                return nextIdx;
            }
        }
        // 没有找到其他武器（只有当前一把）
        return -1;
    }

    public long GetCurrWeaponUID()
    {
        RefreshWeaponActive();
        if (GetSavedItemContainer().ActiveWeaponSlotIndex == -1)
        {
            return -1;
        }

        var uid = OccupiedEquipped[GetSavedItemContainer().ActiveWeaponSlotIndex];
        return uid;
    }

    // 4,5,6,7是武器，位置错误直接返回-1
    public long GetWeaponUID(int location)
    {
        if (location >= 4 && location < OccupiedEquipped.Length)
        {
            return OccupiedEquipped[location];    
        }

        return -1;
    }
    
    // 一定是先更新数据，在更新表现，更新表现一定要先判断是否正确
    public bool SetCurrWeaponUID(long uid)
    {
        var location = -1;
        var hasEquipped = false;
        // 4 5 6 7
        for (var i = 4; i < OccupiedEquipped.Length; i++)
        {
            // 如果还有装备的武器，就不能换成赤手空拳
            if (-1 == uid && OccupiedEquipped[i] > 0)
                return false;

            if (OccupiedEquipped[i] != uid) continue;
            location = i;
            hasEquipped = true;
        }

        if (uid != -1 && !hasEquipped) return false;
        SetCurrentWeaponLocation(location);
        //GetInventoryData().CurrentWeaponLocation = location;
        return true;
    }

    // 界面操作
    public enum AddItemStackToEquipmentResult
    {
        /// <summary>
        /// 操作失败（未知原因）
        /// </summary>
        Failure = 0,

        /// <summary>
        /// 空位置成功装备
        /// </summary>
        SuccessEquipped = 1,

        /// <summary>
        /// 替换已经装备了的
        /// </summary>
        FailureSlotNotEmpty = 2,

        /// <summary>
        /// 失败：装备类型与目标槽位不匹配
        /// </summary>
        FailureTypeMismatch = 3,
    }

    // 界面拖拽操作任何装备到装备栏，都需要将已经装备的先卸下来
    public AddItemStackToEquipmentResult TryAddItemStackToEquipment(long itemStackUID, int location)
    {
        var itemStack = GetItemStackByUID(itemStackUID);
        
        if (!EquipTypeMatch(location, itemStack))
            return AddItemStackToEquipmentResult.FailureTypeMismatch;

        if (location >= OccupiedEquipped.Length)
            return AddItemStackToEquipmentResult.Failure;

        var uid = OccupiedEquipped[location];

        // 如果当前位置为空,则直接加入背包
        if (uid != 0) return AddItemStackToEquipmentResult.FailureSlotNotEmpty;
        // 更新当前装备
        // var firstWeaponLocation = GetFirstWeaponLocation();
        GetSavedItemContainer().Equipped.Add(itemStackUID);
        itemStack.Location = location;
        OccupiedEquipped[location] = itemStackUID;

        OnEquipmentChanged?.Invoke(location, null, itemStack);
        
        return AddItemStackToEquipmentResult.SuccessEquipped;

        // 更换装备的操作，一定是先把槽位上的提前卸下来，然后在把新的装备放上去
        // 所以不可能走到这一步
        // 右键点击装备道具，会先判断有没有空的槽位，所以也不会走到这一步
    }

    private void RefreshWeaponActive()
    {
        var current = GetSavedItemContainer().ActiveWeaponSlotIndex;
        if (current != -1 && OccupiedEquipped[current] != 0) return;
        var newLoc = GetFirstWeaponLocation();
        SetCurrentWeaponLocation(newLoc);
    }

    public bool RemoveItemStackFrontEquipment(long itemStackUID)
    {
        var itemStack = GetItemStackByUID(itemStackUID);
        if (!GetSavedItemContainer().Equipped.Remove(itemStackUID)) return false;
        
        OnEquipmentChanged?.Invoke(itemStack.Location, itemStack, null);
        
        OccupiedEquipped[itemStack.Location] = 0;
        // 这个是没问题的， 非武器不会放到 武器槽位上，但是还是要加个判断
        /*
            if (IsWeaponSlot(itemStack.Location) &&
                GetInventoryData().CurrentWeaponLocation == itemStack.Location)
            {
                GetInventoryData().CurrentWeaponLocation = GetFirstWeaponLocation();
            }*/
        return true;

    }

    // 这个要重新写
    public int GetFirstEmptyWeaponLocation(EquipType equipType)
    {
        if (equipType == EquipType.Weapon)
        {
            for (var i = 4; i < OccupiedEquipped.Length; i++)
            {
                if (OccupiedEquipped[i] == 0)
                    return i;
            }
        }

        return -1;
    }

    private int GetFirstWeaponLocation()
    {
        for (var i = 4; i < OccupiedEquipped.Length; i++)
        {
            if (OccupiedEquipped[i] > 0)
            {
                return i;
            }
        }

        return -1;
    }

    // 防具位置分别在 : 0,1,2,3 
    public int GetArmorLocation(EquipType equipType)
    {
        if (equipType == EquipType.Weapon)
            return -1;

        return (int)equipType - 1;
    }

    public ItemStack GetArmor(EquipType equipType)
    {
        var location = GetArmorLocation(equipType);
        // -1 也查不到任何armor
        var itemContainer = GetSavedItemContainer();
        return itemContainer.Equipped.
            Select(GetItemStackByUID).
            FirstOrDefault(itemStack => itemStack != null && itemStack.Location == location);
    }
    
    private bool EquipTypeMatch(int slot, ItemStack itemStack)
    {
        if (!itemStack.IsEquip())
            return false;
        var equipType = itemStack.GetEquipType();
        if (equipType == EquipType.Weapon && IsWeaponSlot(slot))
        {
            return true;
        }

        if (equipType != EquipType.Weapon)
        {
            return (int)equipType - 1 == slot;
        }

        return false;
    }

    // 4,5,6,7
    private bool IsWeaponSlot(int slot)
    {
        return slot is >= 4 and < 8;
    }

    #endregion
}