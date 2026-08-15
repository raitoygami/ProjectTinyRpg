using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainUI : MonoBehaviour
{
    [SerializeField] private QuickBarWepObj quickBarWepPrefab; 
    private QuickBarWepObj _quickBarWepInst;    
    [SerializeField] private Transform _currentWeaponSlot;
    
    [SerializeField] private List<Transform> _abilities = new();

    [SerializeField] private Image _Portrait;
    [SerializeField] private Image _Health;
    [SerializeField] private Image _Mana;
    [SerializeField] private TMP_Text _HealthValue;
    [SerializeField] private TMP_Text _ManaValue;
    
    public void Start()
    {
        this.SubscribeGlobal<AgentWeapon.EquippedWeaponChangeEvt>(OnEquippedWeaponChangeEvt);
        this.SubscribeGlobal<Context.AvatarChangedEvt>(OnAvatarChanged);
        this.SubscribeGlobal<Context.PlayerHealthChangeEvt>(OnPlayerHealthChange);
    }

    private UniTask OnAvatarChanged(Context.AvatarChangedEvt arg)
    {
        if (!Context.HasInstance() || Context.Instance.PlayerInst == null) return UniTask.CompletedTask;;
        var customization = Context.Instance.PlayerInst.GetComponent<AgentCustomization>();
        if (customization == null) return UniTask.CompletedTask;
        _Portrait.sprite =  customization.GetCombinedSprite();
        return UniTask.CompletedTask;
    }

    public void BindPlayerStat()
    {
        var player = Context.Instance.PlayerInst;
        if (player == null) return;
        
        var agentStat =  player.GetComponent<AgentStats>();
        _HealthValue.text = $"{agentStat.HealthCurrent}/{agentStat.MaxHealth}";
        _Health.fillAmount = (float)agentStat.HealthCurrent / agentStat.MaxHealth;
        
        var customization = Context.Instance.PlayerInst.GetComponent<AgentCustomization>();
        _Portrait.sprite =  customization.GetCombinedSprite();
 
    }

    private UniTask ShieldHandler(AgentStats.ShieldChangedEvent evt)
    {
        /*if (evt.Stats != stats) return UniTask.CompletedTask;
        var max = stats.MaxHealth;
        view.SetShieldFill(max > 0 ? (float)evt.Current / max : 0f);
        view.gameObject.SetActive(stats.HealthCurrent < stats.MaxHealth || evt.Current > 0);*/
        return UniTask.CompletedTask;
    }

    private UniTask OnPlayerHealthChange(Context.PlayerHealthChangeEvt evt)
    {
        var max = evt.Max > 0 ? evt.Max : 1;
        _HealthValue.text = $"{Mathf.Min(evt.Current, max)}/{max}";
        _Health.fillAmount = Mathf.Clamp01((float)evt.Current / max);
        return UniTask.CompletedTask;
    }
    
    private void InstanceActionSlotIcon()
    {
        if (_quickBarWepInst != null) return;
        _quickBarWepInst = Instantiate(quickBarWepPrefab, _currentWeaponSlot);
        _quickBarWepInst.transform.SetSiblingIndex(0);
        _quickBarWepInst.transform.localPosition = Vector3.zero;
        _quickBarWepInst.transform.localRotation = Quaternion.identity;
        _quickBarWepInst.transform.localScale = Vector3.one;
    }
    
    public void OnRefresh()
    {
        InstanceActionSlotIcon();

        var player = Context.Instance.PlayerInst;
        if (player == null)
            return;

        // 获取当前激活的武器
        var agentWeapon = player.GetComponent<AgentWeapon>();
        var weaponActive = agentWeapon.GetWeaponActive();

        var abilityId = weaponActive.WepAtkAbilityId;
        var abilityStat = PlayerManager.Instance.GetAbilityStat(abilityId);
        _quickBarWepInst.UpdateAbilityInfo(abilityId, abilityStat);
        _quickBarWepInst.UpdateIcon(weaponActive.Icon);
    }
    
    private async UniTask OnEquippedWeaponChangeEvt(AgentWeapon.EquippedWeaponChangeEvt arg)
    {
        InstanceActionSlotIcon();
        var abilityId = arg.WeaponChanged.WepAtkAbilityId;
        var abilityStat = PlayerManager.Instance.GetAbilityStat(abilityId);
        _quickBarWepInst.UpdateAbilityInfo(abilityId, abilityStat);
        _quickBarWepInst.UpdateIcon(arg.WeaponChanged.Icon);
        
        await UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        if (_quickBarWepInst != null)
        {
            Destroy(_quickBarWepInst.gameObject);
        }
        _quickBarWepInst = null;
        
    }
}
