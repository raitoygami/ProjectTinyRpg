using System;
using UnityEngine;

public partial class SaveData
{
    // 玩家状态数据
    [Serializable]
    public class PlayerStats
    {
        // 角色Entity ID
        public int EntityID = 0;
        
        public int Level = 1;
        public int Experience = 0;
        
        // 加点信息
        public int StatPoints = 0;
        
        public int AllocSTR = 0;
        public int AllocINT = 0;
        public int AllocVIT = 0;
        public int AllocDEX = 0;
        
        // 场景
        public string WorldName;
        // 位置
        public Vector3 Location;

    }

    public PlayerStats Stats = new();
}

public partial class PlayerManager
{
    public SaveData.PlayerStats GetSavedStats()
    {
        return Persist.Instance.GetState().Stats;
    }

    public int GetEntityID()
    {
        return GetSavedStats().EntityID;
    }
    
    public void SetEntityID(int entityID)
    {
        GetSavedStats().EntityID = entityID;
    }

    public int GetLevel()
    {
        return GetSavedStats().Level;
    }

    public int GetExperience()
    {
        return GetSavedStats().Experience;
    }
    
    public bool AddExp(int exp)
    {
        GetSavedStats().Experience += exp;
        return true;
    }
    
    public int GetStatPoints()
    {
        return GetSavedStats().StatPoints;
    }

    public void SetStatPoints(int value)
    {
        GetSavedStats().StatPoints = value;
    }

    public int GetSTR()
    {
        return GetSavedStats().AllocSTR;
    }

    public void SetSTR(int value)
    {
        GetSavedStats().AllocSTR = value;
    }

    public int GetINT()
    {
        return GetSavedStats().AllocINT;
    }

    public void SetINT(int value)
    {
        GetSavedStats().AllocINT = value;
    }

    public int GetVIT()
    {
        return GetSavedStats().AllocVIT;
    }

    public void SetVIT(int value)
    {
        GetSavedStats().AllocVIT = value;
    }

    public int GetDEX()
    {
        return GetSavedStats().AllocDEX;
    }

    public void SetDEX(int value)
    {
        GetSavedStats().AllocDEX = value;
    }

    public void SetWorld(string worldName)
    {
        GetSavedStats().WorldName = worldName;
    }

    public string GetWorldName()
    {
        return GetSavedStats().WorldName;
    }

    public void SetLocation(Vector3 location)
    {
        GetSavedStats().Location = location;
    }
    
    public Vector3 GetWorldLocation()
    {
        return GetSavedStats().Location;
    }
    
}
