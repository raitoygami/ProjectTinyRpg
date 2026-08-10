using System;
using System.Collections.Generic;
using UnityEngine;

public partial class SaveData
{
    [Serializable]
    public class PlayerLocation
    {
        public string CurrentMap;
        public Vector3 CurrentLocation;

        public Vector3 CurrentDirection = Vector3.one;
        // 在大地图中的Chunk和位置，这个通常用不到，只有从地牢放弃出来的时候，才会用到，但是要时刻记录
        public string CurrentWorld;
        public Vector3 CurrentWorldLocation;
        public Vector3 CurrentWorldDirection = Vector3.one;
    }

    public PlayerLocation _location;

    public PlayerLocation GetLocation()
    {
        _location ??= new PlayerLocation();
        return _location;
    }
}

public partial class PlayerManager
{
    public SaveData.PlayerLocation GetLocation()
    {
        return Persist.Instance.GetPlayerData().GetLocation();
    }
    
        
    public void SetWorld(string sceneName)
    {
        GetLocation().CurrentWorld = sceneName;
    }

    public string GetWorldChunk()
    {
        return GetLocation().CurrentWorld;
    }
    
    public string GetCurrentMap()
    {
        return GetLocation().CurrentMap;
    }

    public void SetCurrentMap(string map)
    {
        GetLocation().CurrentMap = map;
    }
    

    public void SetCurrentLocation(Vector3 location)
    {
        GetLocation().CurrentLocation = location;
    }


    public Vector3 GetCurrentLocation()
    {
        return GetLocation().CurrentLocation;
    }
    
    public Vector3 GetWorldLocation()
    {
        return GetLocation().CurrentWorldLocation;
    }

    public void SetWorldLocation(Vector3 location)
    {
        GetLocation().CurrentWorldLocation = location;
    }
    
}
