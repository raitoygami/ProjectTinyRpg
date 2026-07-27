using System;
using System.Collections.Generic;
using System.Linq;

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
        public long LastWeaponUID = 0;
        public long CurrentWeaponUID = -1;
        // 包裹中的
        public List<ItemStack> InventoryItems = new();
    } 
    
    public InventoryStateData InventoryData = new();
}

public partial class PlayerManager
{
    private GameState.InventoryStateData InventoryData => Persist.Instance.GetState().InventoryData;
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
        OccupiedEquipped = new long[8];

        foreach (var itemStack in InventoryData.InventoryItems)
        {
            if (itemStack.Location < OccupiedInventory.Length)
                OccupiedInventory[itemStack.Location] = itemStack.Uid;
        }

        foreach (var itemStack in InventoryData.EquippedItems)
        {
            if (itemStack.Location < OccupiedEquipped.Length)
                OccupiedEquipped[itemStack.Location] = itemStack.Uid;
        }
        
    }
    // 获取当前装备的Weapon
    public ItemStack GetCurrentWeapon()
    {
        return InventoryData.CurrentWeaponUID != 0 ? 
            InventoryData.EquippedItems.FirstOrDefault(
                itemStack => itemStack.Uid == InventoryData.CurrentWeaponUID
                ) 
            : null;
    }

    public long GetCurrWeaponUID()
    {
        return InventoryData.CurrentWeaponUID;
    }
    
    public long GetLastWeaponUID()
    {
        return InventoryData.LastWeaponUID;
    }
    
    public List<ItemStack> GetEquippedItems()
    {
        return InventoryData.EquippedItems;
    }
    
    
}
