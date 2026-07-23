using System;
using System.Collections.Generic;

/// <summary>
/// 装备栏存档：每槽至多一件；数据在 <see cref="EquipSlotPersistEntry.equipped"/>，与背包桶独立。
/// </summary>
[Serializable]
public class EquipModuleSave
{
    public List<EquipSlotPersistEntry> slots = new();

    public void EnsureAllSlotKeys()
    {
        if (slots == null)
            slots = new List<EquipSlotPersistEntry>();
        /*foreach (EquipSlotType st in Enum.GetValues(typeof(EquipSlotType)))
        {
            if (GetSlotEntryInternal(st) == null)
                slots.Add(new EquipSlotPersistEntry { slotType = (int)st });
        }*/
    }

    /*EquipSlotPersistEntry GetSlotEntryInternal(EquipSlotType t)
    {
        foreach (var e in slots)
        {
            if (e != null && e.slotType == (int)t)
                return e;
        }

        return null;
    }

    public bool TryGetEquippedStack(EquipSlotType t, out ItemStack stack)
    {
        stack = null;
        slots ??= new List<EquipSlotPersistEntry>();
        EnsureAllSlotKeys();
        var e = GetSlotEntryInternal(t);
        if (e?.equipped == null || e.equipped.IsEmpty)
            return false;
        stack = e.equipped;
        return true;
    }

    public void SetEquippedStack(EquipSlotType t, ItemStack stack)
    {
        slots ??= new List<EquipSlotPersistEntry>();
        EnsureAllSlotKeys();
        foreach (var e in slots)
        {
            if (e != null && e.slotType == (int)t)
            {
                e.equipped = stack == null || stack.IsEmpty ? null : stack.Clone();
                return;
            }
        }

        slots.Add(new EquipSlotPersistEntry
        {
            slotType = (int)t,
            equipped = stack == null || stack.IsEmpty ? null : stack.Clone()
        });
    }

    public void ClearSlot(EquipSlotType t) => SetEquippedStack(t, null);

    public void Clear()
    {
        if (slots == null)
        {
            slots = new List<EquipSlotPersistEntry>();
            return;
        }

        foreach (var e in slots)
        {
            if (e != null)
                e.equipped = null;
        }
    }*/
}

[Serializable]
public class EquipSlotPersistEntry
{
    public int slotType;
    public ItemStack equipped;
}
