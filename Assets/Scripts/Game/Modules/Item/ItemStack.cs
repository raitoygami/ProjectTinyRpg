using System;
using cfg;
using Newtonsoft.Json;

/// <summary>
/// 背包/存档中的道具堆（俄罗斯方块格摆放：<see cref="Uid"/>、锚点与 <see cref="Count"/>）。<br/>
/// 堆叠上限：可堆叠 99，不可堆叠 1；<see cref="Uid"/> 与配置 Id 相关：<c>itemId * 1000 + 序号</c>（序号从 1 递增，同 Id 全存档唯一）。
/// </summary>
[Serializable]
public class ItemStack
{
    /// <summary>道具堆实例标识；格式 <c>itemId * 1000 + seq</c>，seq 为同 itemId 下第几堆。</summary>
    public long Uid;

    /// <summary>配置表道具 Id。</summary>
    public int ItemId;

    /// <summary>当前堆叠数量（不超过 <see cref="GetMaxStackCount"/>）。</summary>
    public int Count = 1;

    public int PivotCol;
    public int PivotRow;

    // 背包中的位置
    public int Location;
    
    /// <summary>可堆叠道具单格最大数量。</summary>
    public const int MaxStackableCount = 99;

    [JsonIgnore]
    public bool IsEmpty => ItemId == 0 || Count <= 0;

    public bool StackEquals(ItemStack other)
    {
        return other != null && ItemId != 0 && ItemId == other.ItemId;
    }

    public ItemStack Clone() => new ItemStack
    {
        Uid = Uid,
        ItemId = ItemId,
        Count = Count,
        PivotCol = PivotCol,
        PivotRow = PivotRow
    };

    public string Name()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return def?.Name;
    }

    public string Description()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return def?.Desc;
    }

    public string Category()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return def?.Category;
    }
    
    public bool IsEquip()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return def is t_Equip;
    }
    
    [JsonIgnore]
    public EquipType EquipType => GetEquipType();
    
    public EquipType GetEquipType()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return def is t_Equip e? e.EquipType : default;
    }
    
    public ItemType GetItemType()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return def?.Type ?? ItemType.None;
    }

    public string GetItemAddressable()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return def?.Prefab;
    }
    
    public ItemRarity GetRarity()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return def?.Rarity ?? ItemRarity.Common;
    }
    [JsonIgnore]
    public int Width => GetWidth();
    [JsonIgnore]
    public int Height => GetHeight();
    
    private int GetWidth()
    {
        return 1;
    }
    
    private int GetHeight()
    {
        return 1;
    }

    public bool Stackable()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return def is { Stackable: true };
    }

    public string GetIconAddressable()
    {
        var def = ConfigManager.Instance?.GetItemBase(ItemId);
        return $"{def?.Icon}.png";
    }
    
    public int GetMaxStackCount()
    {
        return GetMaxStackCountForItemId(ItemId);
    }

    public static int GetMaxStackCountForItemId(int itemId)
    {
        if (!ConfigManager.HasInstance())
            return 1;
        var def = ConfigManager.Instance.GetItemBase(itemId);
        return GetMaxStackCountForDef(def);
    }

    public static int GetMaxStackCountForDef(t_ItemBase def)
    {
        if (def == null)
            return 1;
        return def.Stackable ? MaxStackableCount : 1;
    }
}
