using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class SaveData
{
    // 大地图每个thunk的数据
    [Serializable]
    public class MapData
    {
        public Dictionary<Vector3, MapTileData> _fogTiles; // The fog tiles of the map.
        public Dictionary<string, EntityStatData> _entityStats = new();

        public EntityStatData GetEntityStat(string entityName)
        {
            return _entityStats.GetValueOrDefault(entityName);
        }

        public Dictionary<Vector3, MapTileData> GetFogTiles()
        {
            return _fogTiles;
        }
        
        public void SetEntityStat(string entityName, EntityStatData entityStatData)
        {
            if (_entityStats.TryAdd(entityName, entityStatData)) return;
            _entityStats[entityName] = entityStatData;
        }

        public void InitFogTiles(int originX, int originY, int width, int height)
        {
            // 只初始化一次
            if (_fogTiles != null)
                return;
            
            _fogTiles = new Dictionary<Vector3, MapTileData>();
            for (var i = 1; i <= width; i++)
            {
                for (var j = 1; j <= height; j++)
                {
                    var location = new Vector3Int(originX + i, originY + j, 0); // Gets the local position.
                    // Gets the tile.
                    var tile = new MapTileData
                    {
                        isExplored = false,
                        isVisible = false,
                        localPlace = location, // Sets the local place.
                    };
                    _fogTiles.Add(location, tile);
                }
            }
        }

        public MapTileData GetFogTile(Vector3 location)
        {
            return _fogTiles?.GetValueOrDefault(location);
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
