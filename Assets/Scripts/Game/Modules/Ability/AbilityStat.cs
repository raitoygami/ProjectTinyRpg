using System;
using Newtonsoft.Json;

public class AbilityStat
{
    public int AbilityId;
    public int Cooldown;
    
    [JsonIgnore]
    public Action OnCooldownChanged;

}
