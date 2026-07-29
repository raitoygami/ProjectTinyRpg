using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.BuildReportVisualizer;
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

    public AddItemStackToInventoryResult TryAddItemStackToInventory(ItemStack itemStack, int location)
    {
        if (location >= OccupiedInventory.Length)
            return AddItemStackToInventoryResult.InvalidArgument;

        var uid = OccupiedInventory[location];
        // 如果当前位置为空,则直接加入背包
        if (uid == 0)
        {
            GetInventoryData().InventoryItems.Add(itemStack);
            itemStack.Location = location;
            return AddItemStackToInventoryResult.SuccessNewInstance;
        }
        // 当前位置非空

        if (!itemStack.Stackable())
            return AddItemStackToInventoryResult.NotEmptySlot;
            
        var existingItem = GetInventoryData().InventoryItems.FirstOrDefault(item => item.Uid == uid);
        if (existingItem == null)
            return AddItemStackToInventoryResult.InvalidData;

        existingItem.Count += itemStack.Count;
        return AddItemStackToInventoryResult.SuccessStacked;
    }
    
    public AddItemStackToInventoryResult TryAddItemStackToInventory(ItemStack itemStack)
    {
        //  如何可以堆叠，且包裹里有对应物体，则堆叠
        if (itemStack.Stackable())
        {
            var existingItem = GetInventoryData().InventoryItems.First(item => item.ItemId == itemStack.ItemId);
            existingItem.Count += itemStack.Count;
            return AddItemStackToInventoryResult.SuccessStacked;
        }
        // 如果没有，则找空位看看能不能加进去
        var emptySlot = GetFirstEmptySlot();
        if (emptySlot == -1) return  AddItemStackToInventoryResult.Failure;
        
        GetInventoryData().InventoryItems.Add(itemStack);
        OccupiedInventory[emptySlot] = itemStack.Uid;
        
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
                Debug.LogError($"[AddItem] failed: too many items {itemID}");
                return AddItemStackToInventoryResult.InvalidArgument;
            case true when GetInventoryData().InventoryItems.Any(item => item.ItemId == itemID):
            {
                var existingItem = GetInventoryData().InventoryItems.First(item => item.ItemId == itemID);
                itemStack = existingItem;
                existingItem.Count += amount;   // 增加数量
                return AddItemStackToInventoryResult.SuccessStacked;
            }
        }

        var emptySlot = GetFirstEmptySlot();
        if (emptySlot == -1)
            return AddItemStackToInventoryResult.InventoryFull;
        
        itemStack = new ItemStack
        {
            Uid = UidGenerator.Generate(itemID),
            ItemId = itemID,
            Count = amount,
            Location = emptySlot,
        };

        GetInventoryData().InventoryItems.Add(itemStack);
        OccupiedInventory[emptySlot] = itemStack.Uid;
        return AddItemStackToInventoryResult.SuccessNewInstance;
    }

    public bool RemoveItemStackFrontInventory(ItemStack itemStack)
    {
        if (GetInventoryData().InventoryItems.Remove(itemStack))
        {
            OccupiedInventory[itemStack.Location] = 0;
            return true;
        }

        return false;
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
