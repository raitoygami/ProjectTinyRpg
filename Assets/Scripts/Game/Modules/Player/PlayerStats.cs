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

        // runtime dynamic stats
        public int HpLost = 0;
        public int MpLost = 0;

    }
    
    public PlayerStats _stats;

    public PlayerStats GetStats()
    {
        _stats ??= new PlayerStats();
        return _stats;
    }
}

public partial class PlayerManager
{
    public SaveData.PlayerStats GetStats()
    {
        return Persist.Instance.GetPlayerData().GetStats();
    }
    
    public int GetEntityID()
    {
        return GetStats().EntityID;
    }
    
    public void SetEntityID(int entityID)
    {
        GetStats().EntityID = entityID;
    }

    public int GetLevel()
    {
        return GetStats().Level;
    }

    public int GetExperience()
    {
        return GetStats().Experience;
    }
    
    public bool AddExp(int exp)
    {
        GetStats().Experience += exp;
        return true;
    }
    
    public int GetStatPoints()
    {
        return GetStats().StatPoints;
    }

    public void SetStatPoints(int value)
    {
        GetStats().StatPoints = value;
    }

    public int GetSTR()
    {
        return GetStats().AllocSTR;
    }

    public void SetSTR(int value)
    {
        GetStats().AllocSTR = value;
    }

    public int GetINT()
    {
        return GetStats().AllocINT;
    }

    public void SetINT(int value)
    {
        GetStats().AllocINT = value;
    }

    public int GetVIT()
    {
        return GetStats().AllocVIT;
    }

    public void SetVIT(int value)
    {
        GetStats().AllocVIT = value;
    }

    public int GetDEX()
    {
        return GetStats().AllocDEX;
    }

    public void SetDEX(int value)
    {
        GetStats().AllocDEX = value;
    }

}
