using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameState
{
    // 装备在包裹中就存放在InventoryItems
    // 装备已经在装备槽上就存放在EquippedItems
    [Serializable]
    public class InventoryStateData
    {
        // 已经装备的
        public List<ItemStack> EquippedItems = new();
        //
        public long CurrentWeaponUID = -1;
        // 包裹中的
        public List<ItemStack> InventoryItems = new();
    } 
    
    public InventoryStateData InventoryData = new();
}

public partial class PlayerManager
{

    public GameState.InventoryStateData GetInventoryData()
    {
        return Persist.Instance.GetState().InventoryData;
    }
    
    // 列
    public const int InventorySizeCol = 7;
    // 行
    public const int InventorySizeRow = 7;
    
    // 武器槽位就8个 固定
    private long[] OccupiedEquipped;
    // 背包槽位49个，暂定
    private long[] OccupiedInventory;
    
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
        
        foreach (var itemStack in GetInventoryData().InventoryItems)
        {
            if (itemStack.Location < OccupiedInventory.Length)
                OccupiedInventory[itemStack.Location] = itemStack.Uid;
        }

        foreach (var itemStack in GetInventoryData().EquippedItems)
        {
            if (itemStack.Location < OccupiedEquipped.Length)
                OccupiedEquipped[itemStack.Location] = itemStack.Uid;
        }
        
    }
    // 获取当前装备的Weapon
    public ItemStack GetCurrentWeapon()
    {
        return GetInventoryData().CurrentWeaponUID != 0 ? 
            GetInventoryData().EquippedItems.FirstOrDefault(
                itemStack => itemStack.Uid == GetInventoryData().CurrentWeaponUID
                ) 
            : null;
    }

    public long GetCurrWeaponUID()
    {
        return GetInventoryData().CurrentWeaponUID;
    }

    // 一定是先更新数据，在更新表现，更新表现一定要先判断是否正确
    public bool SetCurrWeaponUID(long uid)
    {
        var hasEquipped = false;
        // 4 5 6 7
        for (var i = 4; i < OccupiedEquipped.Length; i++)
        {
            // 如果还有装备的武器，就不能换成赤手空拳
            if (-1 == uid && OccupiedEquipped[i] > 0)
                return false;

            if (OccupiedEquipped[i] == uid)
            {
                hasEquipped = true;
            }
        }

        if (uid != -1 && !hasEquipped) return false;
        GetInventoryData().CurrentWeaponUID = uid;
        return true;

    }
    
    public List<ItemStack> GetEquippedItems()
    {
        return GetInventoryData().EquippedItems;
    }
    
    
    // inventory part

    public bool TryAddItemStack(int itemID, int amount, out ItemStack itemStack)
    {
        itemStack = null;
        
        var config = ConfigManager.Instance.GetItemBase(itemID);
        if (config == null)
        {
            Debug.LogError($"[AddItem] failed: not found item {itemID}");
            return false;
        }

        switch (config.Stackable)
        {
            case false when amount > 1:
                Debug.LogError($"[AddItem] failed: too many items {itemID}");
                return false;
            case true when GetInventoryData().InventoryItems.Any(item => item.ItemId == itemID):
            {
                var existingItem = GetInventoryData().InventoryItems.First(item => item.ItemId == itemID);
                itemStack = existingItem;
                existingItem.Count += amount;   // 增加数量
                return true;
            }
        }

        var emptySlot = GetFirstEmptySlot();
        itemStack = new ItemStack
        {
            Uid = UidGenerator.Generate(itemID),
            ItemId = itemID,
            Count = amount,
            Location = emptySlot,
        };

        GetInventoryData().InventoryItems.Add(itemStack);
        OccupiedInventory[emptySlot] = itemStack.Uid;
        return true;
    }

    private int GetFirstEmptySlot()
    {
        for (var i = 0; i < OccupiedInventory.Length; i++)
        {
            if (OccupiedInventory[i] == 0)
                return i;
        }

        return -1;
    }
    
    
}
