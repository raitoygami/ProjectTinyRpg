[Panel("Inventory", "UI/InventoryUI", "HUDRight", MuteGroup = "HUDRight", EscBehavior =  EscBehavior.CloseOnly)]
public class InventoryUI : PanelBase
{
    public override void Close()
    {
        UIRoot.Instance.ToolTipUI.HideTip();
        base.Close();
    }
}
