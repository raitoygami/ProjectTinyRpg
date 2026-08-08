using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class NavigationSettings : ScriptableObject{
    [SerializeField] public GameObject NavigationMarkEnd;
    
    // 技能范围相关
    [SerializeField] public RuleTile TileAbilityRange;
    [SerializeField] public Tile TileAbilityCastRange;
    [SerializeField] public Tile TileAbilityTelegraph;

}
