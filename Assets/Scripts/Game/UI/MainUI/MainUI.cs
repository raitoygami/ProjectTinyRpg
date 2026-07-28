using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MainUI : MonoBehaviour
{
    [SerializeField] private ActionSlotIconObj ActionSlotIconPrefab; 
    private ActionSlotIconObj _ActionSlotIconInst;    
    [SerializeField] private Transform _currentWeaponSlot;
    
    [SerializeField] private List<Transform> _abilities = new();
    [SerializeField] private Transform _quickItemSlot1;
    [SerializeField] private Transform _quickItemSlot2;

    public void Start()
    {
        this.SubscribeGlobal<AgentWeapon.EquippedWeaponChangeEvt>(OnEquippedWeaponChangeEvt);
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
