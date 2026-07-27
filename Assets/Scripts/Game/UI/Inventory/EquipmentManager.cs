using System;
using System.Collections.Generic;
using cfg;

public partial class GameState
{
    public readonly EquipmentState Equipment = new ();

    [Serializable]
    public class EquipmentState
    {
        public Dictionary<long, ItemStack> ItemStacks = new();
    }
    
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
        return RuntimeState.ItemStacks.Remove(holdItem.Uid);
    }
    
    public bool ReturnItemStackToOriginal(ItemStack item)
    {
        return RuntimeState.ItemStacks.ContainsKey(item.Uid) && Occupied.TryAdd(item.EquipType, item);
    }
    
}
