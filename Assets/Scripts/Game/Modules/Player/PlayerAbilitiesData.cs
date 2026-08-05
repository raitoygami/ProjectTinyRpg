using System;
using System.Collections.Generic;
using UnityEngine;


public partial class SaveData
{
    public class PlayerAbilities
    {
        // 技能数据
        public Dictionary<int, AbilityStat> LookupTable = new();

        public AbilityStat GetAbilityStat(int abilityId)
        {
            LookupTable ??= new Dictionary<int, AbilityStat>();
            
            if (!LookupTable.TryGetValue(abilityId, out var abilityStat))
            {
                abilityStat = new AbilityStat(){Cooldown = 0, AbilityId =  abilityId};
                LookupTable.Add(abilityId, abilityStat);
            }
            return abilityStat;
        }
    }
    
    public PlayerAbilities _abilities;

    public PlayerAbilities GetAbilities()
    {
        _abilities ??= new PlayerAbilities();
        return _abilities;
    }
    
}

public partial class PlayerManager
{
    public SaveData.PlayerAbilities GetAbilities()
    {
        return Persist.Instance.GetPlayerData().GetAbilities();
    }

    public AbilityStat GetAbilityStat(int abilityId)
    {
        return GetAbilities().GetAbilityStat(abilityId);
    }
    
}
