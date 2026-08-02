using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainUI : MonoBehaviour
{
    [SerializeField] private ActionSlotIconObj ActionSlotIconPrefab; 
    private ActionSlotIconObj _ActionSlotIconInst;    
    [SerializeField] private Transform _currentWeaponSlot;
    
    [SerializeField] private List<Transform> _abilities = new();
    [SerializeField] private Transform _quickItemSlot1;
    [SerializeField] private Transform _quickItemSlot2;

    [SerializeField] private Image _Health;
    [SerializeField] private Image _Mana;
    [SerializeField] private TMP_Text _HealthValue;
    [SerializeField] private TMP_Text _ManaValue;
    
    public void Start()
    {
        this.SubscribeGlobal<AgentWeapon.EquippedWeaponChangeEvt>(OnEquippedWeaponChangeEvt);
    }

    public void BindPlayerStat()
    {
        var player = Context.Instance.PlayerInst;
        var agentStat =  player.GetComponent<AgentStats>();
       
        _HealthValue.text = $"{agentStat.HealthCurrent}/{agentStat.MaxHealth}";
        _Health.fillAmount = (float)agentStat.HealthCurrent / agentStat.MaxHealth;
        if (player == null) return;
        var pub = player.GetComponent<PubSubActor>();
        if (pub == null) return;

        pub.Messager.Subscribe<AgentStats.HealthChangedEvent>(Handler);
        pub.Messager.Subscribe<AgentStats.ShieldChangedEvent>(ShieldHandler);
    }

    private UniTask ShieldHandler(AgentStats.ShieldChangedEvent evt)
    {
        /*if (evt.Stats != stats) return UniTask.CompletedTask;
        var max = stats.MaxHealth;
        view.SetShieldFill(max > 0 ? (float)evt.Current / max : 0f);
        view.gameObject.SetActive(stats.HealthCurrent < stats.MaxHealth || evt.Current > 0);*/
        return UniTask.CompletedTask;
    }

    private UniTask Handler(AgentStats.HealthChangedEvent evt)
    {
        var max = evt.Max > 0 ? evt.Max : 1;
        _HealthValue.text = $"{Mathf.Min(evt.Current, max)}/{max}";
        _Health.fillAmount = Mathf.Clamp01((float)evt.Current / max);
        return UniTask.CompletedTask;
    }
    
    private void InstanceActionSlotIcon()
    {
        if (_ActionSlotIconInst == null)
        {
            _ActionSlotIconInst = Instantiate(ActionSlotIconPrefab, _currentWeaponSlot);
            _ActionSlotIconInst.transform.SetSiblingIndex(0);
            _ActionSlotIconInst.transform.localPosition = Vector3.zero;
            _ActionSlotIconInst.transform.localRotation = Quaternion.identity;
            _ActionSlotIconInst.transform.localScale = Vector3.one;
        }
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
        _ActionSlotIconInst.UpdateIcon(weaponActive.Icon);
    }
    
    private async UniTask OnEquippedWeaponChangeEvt(AgentWeapon.EquippedWeaponChangeEvt arg)
    {
        InstanceActionSlotIcon();
        _ActionSlotIconInst.UpdateIcon(arg.WeaponChanged.Icon);
        
        await UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        if (_ActionSlotIconInst != null)
        {
            Destroy(_ActionSlotIconInst.gameObject);
        }
        _ActionSlotIconInst = null;
        
    }
}
