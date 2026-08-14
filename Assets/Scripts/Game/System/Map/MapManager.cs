using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class SaveData
{
    // 大地图每个thunk的数据
    [Serializable]
    public class MapData
    {
        public int OriginX;
        public int OriginY;
        public int Width;
        public int Height;
        // 战争迷雾数据 tilemap的范围是 [OriginX + 1 -> OriginX + Width], 所以_fovData需要多出来一行
        public byte[,] _fovData;
        
        public Dictionary<string, EntityStatData> _entityStats = new();

        public EntityStatData GetEntityStat(string entityName)
        {
            return _entityStats.GetValueOrDefault(entityName);
        }

        public bool WithinRange(Vector3Int location)
        {
            // 如果 _fogData 未初始化，直接返回 false
            if (_fovData == null) return false;
            // 将世界坐标转换为数组索引
            var xIndex = location.x - OriginX;
            var yIndex = location.y - OriginY; // 使用 location.y 对应 Z 轴（因为地图是二维平面）
            // 检查索引是否在数组边界内
            return xIndex >= 0 && xIndex <= Width && yIndex >= 0 && yIndex <= Height;
        }
        
        public void SetFOV(Vector3Int location, bool isExplored)
        {
            if (!WithinRange(location))
                return;
            _fovData[location.x - OriginX, location.y - OriginY] = (byte)(isExplored ? 1 : 0);
        }

        public bool GetFOV(Vector3Int location)
        {
            return WithinRange(location) && _fovData[location.x - OriginX, location.y - OriginX] == 1;
        }

        public void SetEntityStat(string entityName, EntityStatData entityStatData)
        {
            if (_entityStats.TryAdd(entityName, entityStatData)) return;
            _entityStats[entityName] = entityStatData;
        }

        public void InitFogTiles(int originX, int originY, int width, int height)
        {
            if (_fovData != null)
                return;

            OriginX = originX;
            OriginY = originY;
            Width = width;
            Height = height;

            _fovData = new byte[width + 1, height + 1];

            for (var i = 0; i <= width; i++)
            for (var j = 0; j <= height; j++)
                _fovData[i, j] = 0;
        }
        
    }
    
    public Dictionary<string, MapData> _maps = new();

    public Dictionary<string, MapData> GetMapData()
    {
        return _maps;
    }

    public MapData GetMapData(string sceneName)
    {
        if (!_maps.TryGetValue(sceneName, out var mapData))
        {
            mapData = new MapData();
            _maps.Add(sceneName, mapData);
        }
        return mapData;
    }
    // 大地图全局数据 存放一些怪物
    // 场景中的所有怪物，都通过spawner摆放实现，所以
}

public partial class MapManager : Singleton<MapManager>
{
    public SaveData.MapData GetMapData(string sceneName)
    {
        return Persist.Instance.GetPlayerData().GetMapData(sceneName);
    }
    
    private MapConfig _MapConfig;
    
    public async UniTask LoadMapInfo()
    {
        var handle = Addressables.LoadAssetAsync<MapConfig>("ScriptableObject/MapConfig");
        await handle.ToUniTask();

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var config = handle.Result;
            _MapConfig = Instantiate(config);
        }
    }

    public MapConfig.MapInfo GetMapInfo(string sceneName)
    {
        return _MapConfig.GetConfigBySceneName(sceneName);
    }

    public MapConfig.MapInfo GetChunkInfo(Vector2Int chunkIndex)
    {
        return _MapConfig.GetConfigByChunkIndex(chunkIndex);
    }
    
    public EntityStatData GetEntityStatData(string sceneName, string entityName)
    {
        var mapInfo = GetMapInfo(sceneName);
        if (mapInfo == null)
        {
            Debug.LogError($"Map info {sceneName} doesn't exist.");
            return null;
        }

        var mapData = GetMapData(sceneName);
        if (mapData == null)
        {
            Debug.LogError($"Map Data {sceneName} doesn't exist.");
            return null;
        }

        return mapData.GetEntityStat(entityName);

    }

    public bool SetEntityStatData(string sceneName, string entityName, EntityStatData entityStatData)
    {
        var mapInfo = GetMapInfo(sceneName);
        if (mapInfo == null)
        {
            Debug.LogError($"Map info {sceneName} doesn't exist.");
            return false;
        }

        var mapData = GetMapData(sceneName);
        if (mapData == null)
        {
            Debug.LogError($"Map Data {sceneName} doesn't exist.");
            return false;
        }
        mapData.SetEntityStat(entityName, entityStatData);
        return true;
    }

}
