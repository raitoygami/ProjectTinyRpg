using cfg;

/// <summary>
/// 全局 Luban 配置表入口；在 <see cref="Game"/> 加载完 JSON 后 <see cref="Init"/> 一次。
/// </summary>
public class ConfigManager : Singleton<ConfigManager>
{
    private Tables _tables;

    public void Init(Tables tables) => _tables = tables;

    /// <summary>当前配置表；未 Init 前为 null。</summary>
    public Tables Tables => _tables;

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
}
