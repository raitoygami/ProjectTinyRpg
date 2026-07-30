using UnityEngine;

[Panel("Inventory", "UI/InventoryUI", "HUDRight", MuteGroup = "HUDRight", EscBehavior =  EscBehavior.CloseOnly)]
public class InventoryUI : PanelBase
{
    [SerializeField] private InventoryPanel _inventoryPanel;
    [SerializeField] private EquipmentPanel _equipmentPanel;

    public InventoryPanel GetInventoryPanel()
    {
         return _inventoryPanel; 
    }

    public EquipmentPanel GetEquipmentPanel()
    {
        return _equipmentPanel;
    }
    
    
    public override void Close()
    {
        UIRoot.Instance.ToolTipUI.HideTip();
        base.Close();
    }
}
