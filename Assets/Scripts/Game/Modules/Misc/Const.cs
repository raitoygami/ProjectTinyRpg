using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Const
{
    public static class Layer
    {
        
        public static LayerMask BlockOnly = LayerMask.GetMask("Obstacle");
        public static LayerMask WaterOnly = LayerMask.GetMask("Water");
        public static LayerMask GrassOnly = LayerMask.GetMask("Grass");
        public static LayerMask ObstacleOnly = LayerMask.GetMask("Obstacle", "Water");
        public static LayerMask ObstacleForNavi = LayerMask.GetMask("Obstacle", "Water", "Creature", "Interact", "Player");
        public static LayerMask ForInteractHover = LayerMask.GetMask("Interact");
        public static readonly LayerMask ObstacleForEnemyNavi = LayerMask.GetMask("Obstacle", "Water", "Interact");
        public static readonly LayerMask LayerFogComputeFOV = LayerMask.GetMask("Obstacle", "Interact");
    }

    public static class KeyUI
    {
        public const string Inventory = "Inventory";
        public const string Stats = "Stats";
        public const string SettingPanel = "Settings";
        public const string Overworld = "Overworld";
    }

    public static class LocalizationTable
    {
        public const string Equipment = "Equipment";
        public const string Item =  "Item";
    }

    public static class ShaderPropertyKey
    {
        public static readonly int PlayerLocation = Shader.PropertyToID("_PlayerLocation");
        public static readonly int CursorLocation = Shader.PropertyToID("_CursorLocation");
        public static readonly int TexelSizeX = Shader.PropertyToID("_OutlineTexelSizeX");
        public static readonly int TexelSizeY = Shader.PropertyToID("_OutlineTexelSizeY");
        public static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");
        public static readonly int DissolveClip = Shader.PropertyToID("_DissolveClip");
        public static readonly int Fade = Shader.PropertyToID("_Fade");
    }
    
}