using System.Collections;
using System.Collections.Generic;
using skner.DualGrid;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TileAssetTable : ScriptableObject{
    [SerializeField] public GameObject NavigationMarkEnd;
    
    [Header("Abilities Indicator")]
    // 技能范围相关
    [SerializeField] public RuleTile TileAbilityCastRange;
    [SerializeField] public Tile TileAbilityAffectRange;
    [SerializeField] public Tile TileAbilityTelegraph;

    [Header("FOV")] 
    [SerializeField] public DualGridRuleTile DualTileFog;
    [SerializeField] public DualGridRuleTile DualTileView;

    [SerializeField] public Material DualTileFogMaterial;
    [SerializeField] public Material DualTileViewMaterial;

    // block l
    [Header("Block Layer")]
    public Tile TileWater;
    public Tile TileGrass;
    public Tile TileBlock;
    
    // 使用字典存储 瓦片名称 -> Layer
    private Dictionary<string,  LayerMask> tileLayerMap;
    
    private void Init()
    {
        tileLayerMap = new Dictionary<string, LayerMask>
        {
            { TileWater.name, Const.Layer.WaterOnly },
            { TileGrass.name, Const.Layer.GrassOnly }
            // 后续可以扩展
        };
    }

    // 对外提供查询方法
    public LayerMask GetLayerByTile(TileBase tile)
    {
        if (tileLayerMap == null)
            Init();
        if (tile != null && tileLayerMap != null && tileLayerMap.TryGetValue(tile.name, out var layer))
            return layer;
        return Const.Layer.BlockOnly; // 默认
    }
    
}
