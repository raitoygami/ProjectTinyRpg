using Cysharp.Threading.Tasks;
using UnityEngine;

public class E_Damage : AbilityEffect
{
    [SerializeField] private DamageType damageType;
    [SerializeField] private int skillMultiplier;
    
    protected override async UniTask OnApply()
    {
        // 延迟结算或连段中目标/施法者可能已 Destroy，须先按 Unity 假 null 判断再 GetComponent。
        if (!m_Context.Owner || !m_Context.Target)
            return;

        var sourceStats = m_Context.Owner.GetComponent<AgentStats>();
        if (sourceStats == null)
            return;
        var targetStats = m_Context.Target.GetComponent<AgentStats>();
        if (targetStats == null)
            return;

        await sourceStats.DealDamage(targetStats, damageType, skillMultiplier);
        
        await ApplyChildren();
    }
}
