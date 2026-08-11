using System;
using System.Collections.Generic;
using UnityEngine;

public partial class SaveData
{
    [Serializable]
    public class PlayerLocation
    {
        public string SceneName;
        public Vector3 Location;
        public Vector3 Direction = Vector3.one;
        // 在大地图中的Chunk和位置，这个通常用不到，只有从地牢放弃出来的时候，才会用到，但是要时刻记录
        public string WorldName;
        public Vector3 WorldLocation;
        public Vector3 WorldDirection = Vector3.one;
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

    public static string GetSceneName()
    {
        var location = GetLocation();
        return location.SceneName;
    }
    
    public static SaveData.PlayerLocation GetLocation()
    {
        return Persist.Instance.GetPlayerData().GetLocation();
    }
    
        
    public void SetWorld(string sceneName)
    {
        GetLocation().WorldName = sceneName;
    }

    public string GetWorldChunk()
    {
        return GetLocation().WorldName;
    }
    
    public string GetCurrentMap()
    {
        return GetLocation().SceneName;
    }

    public void SetCurrentMap(string map)
    {
        GetLocation().SceneName = map;
    }
    

    public void SetCurrentLocation(Vector3 location)
    {
        GetLocation().Location = location;
    }


    public Vector3 GetCurrentLocation()
    {
        return GetLocation().Location;
    }
    
    public Vector3 GetWorldLocation()
    {
        return GetLocation().WorldLocation;
    }

    public void SetWorldLocation(Vector3 location)
    {
        GetLocation().WorldLocation = location;
    }
    
}
