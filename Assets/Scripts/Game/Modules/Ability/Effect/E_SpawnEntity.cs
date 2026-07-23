using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 在技能选择的格子 <see cref="AbilityContext.Position"/> 处，按实体表 id 召唤单位。
/// 召唤物阵营随施法者（玩家侧 → <see cref="EntityFaction.PlayerSummon"/>，敌方 → <see cref="EntityFaction.EnemySummon"/>）。
/// </summary>
[AbilityEffectMenu("Meta/SpawnEntity")]
public class E_SpawnEntity : AbilityEffect
{
    [SerializeField] private int entityId;
    [Tooltip("存在回合数：召唤物每次轮到自身行动开始时递减，归零后于下一次轮到前移除。")]
    [SerializeField] private int lifetimeTurns = 3;

    protected override UniTask OnApply()
    {
        var em = EntityManager.Instance;
        if (em == null || entityId <= 0)
            return ApplyChildren();
        var owner = m_Context.Owner;
        if (owner == null)
            return ApplyChildren();
        
        var spawnGrid = m_Context.Position;
        var faction = EntityManager.GetSummonFactionForOwner(owner.Faction);
        em.CreateAIEntitySummon(spawnGrid, entityId, faction, lifetimeTurns, owner);

        return ApplyChildren();
    }
}
