using System.Collections.Generic;
using JSAM;
using UnityEngine;

/// <summary>
/// 预加载资源（ScriptableObject）。运行时通过 Addressables 地址 <c>Settings/GamePreload</c> 加载；<see cref="Game"/> 仅从此读取引用。
/// </summary>
[CreateAssetMenu(fileName = "GamePreload", menuName = "Game/Game Preload", order = 0)]
public class GamePreload : ScriptableObject
{
    [Header("主相机")]
    public GameObject mainCameraPrefab;
    public GameObject followCameraPrefab;
    public GameObject uiRoot;

    public GameObject PlayerTemplate;
    public GameObject EnemyTemplate;
    
    [Header("背包（俄罗斯方块数据层：总格位与列数；由运行时写入 InventoryModuleSave，不随 InventoryPanel UI 自动同步）")]
    [Tooltip("总格位数（如 8 列×4 行=32）。须与你在界面里摆的格子总数一致，由你自行对齐布局。")]
    public int InventorySlotCount = 32;

    [Tooltip("列数。0 表示与 TetrisInventorySimulator 默认一致（8 列）。")]
    public int InventoryGridColumns = 8;
    
    public AudioLibrary AudioLibrary;
    
}
