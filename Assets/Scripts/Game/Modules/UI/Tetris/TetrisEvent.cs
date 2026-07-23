using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TryAddItemEvt : EventArgs
{
    public ItemStack Item { get; }
    public bool Success { get; }

    public TryAddItemEvt(ItemStack item, bool success)
    {
        Item = item;
        Success = success;
    }
}