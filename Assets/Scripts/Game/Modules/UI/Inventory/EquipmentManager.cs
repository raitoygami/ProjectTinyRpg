using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using cfg;
using UnityEngine;

public partial class GameState
{
    [Serializable]
    public class EquipmentState
    {
        public Dictionary<long, ItemStack> ItemStacks = new();
    }
    public EquipmentState Equipment = new ();

}

public class EquipmentManager : Singleton<EquipmentManager>
{
    // persist
    private GameState.EquipmentState RuntimeState => Persist.Instance.GetState().Equipment;
    public IEnumerable<ItemStack> AllItems => RuntimeState.ItemStacks.Values;
    
    private readonly Dictionary<EquipType, ItemStack> Occupied = new();

    public override void Initialized()
    {
        RebuildOccupied();
    }

    private void RebuildOccupied()
    {
        // 清空占用表
        Occupied.Clear();

        foreach (var item in RuntimeState.ItemStacks.Values)
        {
            Occupied.Add(item.EquipType, item);
        }
    }
    
    public void PickUpItem(long uid)
    {
        if (!RuntimeState.ItemStacks.TryGetValue(uid, out var item))
            return;

        Occupied.Remove(item.EquipType);
    }
    
    public bool TryDropItem(ItemStack holdItem, out ItemStack swappedItem)
    {
        swappedItem = null;

        if (Occupied.TryGetValue(holdItem.EquipType, out var itemStack))
        {
            swappedItem = itemStack;
            /*_itemStacks.Remove(itemStack);*/
            Occupied.Remove(itemStack.EquipType);
        }
        
        RuntimeState.ItemStacks.Add(holdItem.Uid, holdItem);
        Occupied.Add(holdItem.EquipType, holdItem);
        return true;
    }
    
    public bool RemoveItemStack(ItemStack holdItem)
    {
        if (!RuntimeState.ItemStacks.ContainsKey(holdItem.Uid)) return false;
        RuntimeState.ItemStacks.Remove(holdItem.Uid);
        return true;
    }
    
    public bool ReturnItemStackToOriginal(ItemStack item)
    {
        if (!RuntimeState.ItemStacks.ContainsKey(item.Uid))
            return false;

        if (Occupied.ContainsKey(item.EquipType))
            return false;

        Occupied.Add(item.EquipType, item);
        return true;
    }
    
    
}
