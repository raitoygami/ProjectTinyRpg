using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "MapConfig", menuName = "Config/Map Config")]
public class MapConfig : ScriptableObject
{
    public enum MapType
    {
        WorldChunk,
        Dungeon,
    }
    
    [Serializable]
    public class MapInfo
    {
        public string SceneName;
        public MapType MapType;
        public Vector2Int ChunkIndex;
        public string AddressableName;
        public GameAudioMusic MainMusic;
    }
    [SerializeField] private List<MapInfo> _configs = new();

    // 运行时字典（缓存）
    private Dictionary<string, MapInfo> _runtimeDict;

    // 在游戏启动时调用一次（比如在 MapManager 里）
    public void Initialize()
    {
        _runtimeDict = new Dictionary<string, MapInfo>();
        foreach (var mapInfo in _configs.Where(map => map.SceneName != string.Empty))
        {
            if (!_runtimeDict.TryAdd(mapInfo.SceneName, mapInfo))
            {
                Debug.LogWarning($"MapConfig 中存在重复的 SceneName: {mapInfo.SceneName}，已跳过。");   
            }
        }
    }
    
    public MapInfo GetConfigBySceneName(string sceneName)
    {
        if (_runtimeDict == null) Initialize();
        return _runtimeDict?.GetValueOrDefault(sceneName);
    }
    
}
