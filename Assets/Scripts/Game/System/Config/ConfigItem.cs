using cfg;

public partial class ConfigManager
{
    
    public t_Item GetItem(int itemID)
    {
        return Tables?.DataItem.GetOrDefault(itemID);
    }

    /// <summary>
    /// 先查装备表再查道具表（共用 id 时以装备为准）；用于俄罗斯方块背包等需 <see cref="t_ItemBase.X"/> / <see cref="t_ItemBase.Y"/> 的配置。
    /// </summary>
    public t_ItemBase GetItemBase(int itemId)
    {
        if (Tables == null)
            return null;
        var equip = Tables.DataEquip.GetOrDefault(itemId);
        if (equip != null)
            return equip;
        return Tables.DataItem.GetOrDefault(itemId);
    }

    public t_Drop GetDrop(int dropID)
    {
        return Tables?.DataDrop.GetOrDefault(dropID);
    }

}
