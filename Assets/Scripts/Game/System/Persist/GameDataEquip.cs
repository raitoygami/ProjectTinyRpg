/// <summary><see cref="GameData"/> 的装备栏字段与初始化。</summary>
public partial class GameData
{
    public EquipModuleSave equip;

    public EquipModuleSave EnsureEquipModule()
    {
        if (equip == null)
            equip = new EquipModuleSave();
        equip.EnsureAllSlotKeys();
        return equip;
    }

    private static readonly int _registerEquipSerialization = RegisterEquipSerialization();

    private static int RegisterEquipSerialization()
    {
        RegisterSerializationCallbacks(
            d => d.EnsureEquipModule(),
            d => d.EnsureEquipModule());
        return 0;
    }
}
