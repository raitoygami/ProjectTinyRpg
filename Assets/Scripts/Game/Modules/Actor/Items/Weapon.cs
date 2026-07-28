using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;

public enum WeaponType
{
    Unarmed = 0,
    OneHand = 1, //单手武器 剑盾 : 高防御、格挡、均衡
    Polearms = 2,  // 长柄 : 长棍 长枪一类  : 长距离、穿刺/横扫、控场牵制
    HeavyBlunt = 3, // 重锤 高伤、破甲、钝击、击退 眩晕
    Ranged = 4, // 远程武器 弓 弩 特性 风筝、物理输出、精准打击
    Magic = 5, // 法术武器 魔杖，魔法书 元素伤害、施法、智力依赖 范围攻击
    DualWield = 6, // 短刃 极速攻击、暴击、背刺、灵活
}

public abstract class Weapon : MonoBehaviour
{
    public WeaponType Type;
    
    [SerializeField] public LocalizedString AbilityName;
    [SerializeField] public Sprite Icon;
    
    public Ability AbilityNormalAtk;

    private Ability _NormalAtk;
    // 获取普通攻击实例, 普同攻击也可能有CD，切换武器的时候，也要保留武器的普通攻击状态,不能每次都实例化一个技能实体
    public Ability GetNormalAtk()
    {
        if (_NormalAtk == null &&  AbilityNormalAtk != null)
        {
            _NormalAtk = Instantiate(AbilityNormalAtk);
        }
        return _NormalAtk;
    }
    
    public virtual void Equipped(AgentWeapon agentWeapon)
    {
        
    }

    public virtual void Unequip(AgentWeapon agentWeapon)
    {
        
    }
    
    public virtual async UniTask Startup(Vector2 direction, float duration)
    {
        await UniTask.NextFrame();
    }

    public virtual async UniTask Attack(Vector2 direction, float duration)
    {
        await UniTask.NextFrame();
    }

    public virtual async UniTask Recovery(float duration)
    {
        await UniTask.NextFrame();
    }
    
}
