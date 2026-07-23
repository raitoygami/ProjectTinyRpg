using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityFaction
{
    Neutral = 0,
    Player = 1,
    Enemy = 2,
    Animal = 3,
    Env = 5,
    /// <summary>玩家侧召唤物，与 <see cref="EntityFaction.Enemy"/>、<see cref="EntityFaction.EnemySummon"/> 敌对。</summary>
    PlayerSummon = 6,
    /// <summary>敌方召唤物，与 <see cref="Player"/>、<see cref="PlayerSummon"/> 敌对。</summary>
    EnemySummon = 7,
}
