using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Const
{
    public static class Layer
    {
        public static LayerMask ObstacleOnly = LayerMask.GetMask("Obstacle");
        public static LayerMask ObstacleForNavi = LayerMask.GetMask("Obstacle", "Creature", "Interact", "Player");
        public static readonly LayerMask LayerMaskInteract = LayerMask.GetMask( "Interact");
        public static LayerMask ForLootCover = LayerMask.GetMask("Loot");
        public static readonly LayerMask ObstacleForEnemyNavi = LayerMask.GetMask("Obstacle", "Interact");
    }

    public static class KeyUI
    {
        public const string Inventory = "Inventory";
        public const string Overworld = "Overworld";
        public const string SettingPanel = "Settings";
    }
    
}