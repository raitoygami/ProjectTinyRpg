using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class TileAssetTable : ScriptableObject{
    [SerializeField] public GameObject NavigationMarkEnd;
    
    [Header("Abilities Indicator")]
    // 技能范围相关
    [SerializeField] public RuleTile TileAbilityRange;
    [SerializeField] public Tile TileAbilityCastRange;
    [SerializeField] public Tile TileAbilityTelegraph;

    [Header("FOV")] 
    [SerializeField] public RuleTile TileFOV;
}
