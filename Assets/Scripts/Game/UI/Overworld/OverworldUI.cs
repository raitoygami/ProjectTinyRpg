using UnityEngine;

[Panel("Overworld", "UI/OverworldUI", "Overlay", MuteGroup = "Overlay", EscBehavior =  EscBehavior.CloseOnly)]
public class OverworldUI : PanelBase
{
    [SerializeField] private RectTransform _context;
    
    [SerializeField] private TilemapUI _layerBot;
    [SerializeField] private TilemapUI _layerMid;
    [SerializeField] private TilemapUI _layerTop;
    
    [SerializeField] public Overworld _overworld;
    
    private void Awake()
    {
        var layerBot = _overworld.LayerBot;
        var bounds = layerBot.cellBounds;
        const int tileSize = 64;
        
        _context.sizeDelta = new Vector2(bounds.size.x * tileSize, bounds.size.y * tileSize);
        _layerBot.Initialize(_overworld.LayerBot, bounds, tileSize, tileSize);
        _layerMid.Initialize(_overworld.LayerMid, bounds, tileSize, tileSize);
        _layerTop.Initialize(_overworld.LayerTop, bounds, tileSize, tileSize);
    }
}
