using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AgentWeapon : MonoBehaviour
{
    [SerializeField] private Transform _weaponBack;
    [SerializeField] private Transform _weaponFront;

    [SerializeField] private Weapon _unarmedWeapon;
    
    public class EquippedWeaponChangeEvt : EventArgs
    {
        public Weapon WeaponChanged;
        public Ability WepNormalAtk;
    }

    private Weapon _unarmedWeaponInst;
    private long _currentWeaponUID = 0;
    private readonly Dictionary<long, Weapon> _weapons = new();

    public Weapon GetWeaponActive()
    {
        return _weapons.GetValueOrDefault(_currentWeaponUID, null);
    }
    
    // 
    public async UniTask SwapWeapon(long currEquippedWeaponUID)
    {
        var lastEquippedWeaponUID = _currentWeaponUID;
        // 1: 武器
        if (lastEquippedWeaponUID == currEquippedWeaponUID)
            return;
 
        //  卸载上次的装备
        if (_weapons.TryGetValue(lastEquippedWeaponUID, out var lastWeapon))
        {
            lastWeapon.Unequip(this);
        }

        _currentWeaponUID = currEquippedWeaponUID;
        // 通过界面操作。把所有武器全部卸掉
        // 
        if (_currentWeaponUID == -1)
        {
            // 还没有实例化过赤手空拳
            if (_unarmedWeaponInst == null)
            {
                _unarmedWeaponInst = Instantiate(_unarmedWeapon, transform);
                _weapons.Add(-1, _unarmedWeaponInst);
            }
            _unarmedWeaponInst.Equipped(this);
            await this.Publish(new EquippedWeaponChangeEvt() { WepNormalAtk = _unarmedWeaponInst.GetNormalAtk(), WeaponChanged = _unarmedWeaponInst});
            return;
        }
        
        var currentWeapon = PlayerManager.Instance.GetCurrentWeapon();
        if (currentWeapon == null)
        {
            Debug.LogError("不可能吧");
            return;
        }

        if (_weapons.TryGetValue(currentWeapon.Uid, out var weapon))
        {
            weapon.Equipped(this);
            await this.Publish(new EquippedWeaponChangeEvt() { WepNormalAtk = weapon.GetNormalAtk(), WeaponChanged = weapon});
        }
        else
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(currentWeapon.GetItemAddressable());
            await handle.ToUniTask();
        
            var weaponInst =  Instantiate(handle.Result, transform).GetComponent<Weapon>() ;
            weaponInst.Equipped(this);
            _weapons.Add(currentWeapon.Uid, weaponInst);
            await this.Publish(new EquippedWeaponChangeEvt() { WepNormalAtk = weaponInst.GetNormalAtk(), WeaponChanged = weaponInst});
        }
        
        await UniTask.CompletedTask;
    }

    public void LoadEnemyWeapon(Weapon t_Weapon)
    {
        var weapon = Instantiate(t_Weapon) ;
        weapon.Equipped(this);
        _currentWeaponUID = 0;
        _weapons.Add(_currentWeaponUID, weapon);
    }

    public Weapon WeaponCurrent()
    {
        return _weapons.GetValueOrDefault(_currentWeaponUID, _unarmedWeaponInst);
    }

    public Transform FrontSlot()
    {
        return _weaponFront;
    }

    public Transform BackSlot()
    {
        return _weaponBack;
    }

    private void OnDestroy()
    {
        foreach (var weapon in _weapons)
        {
            if (weapon.Value.gameObject != null)
            {
                Destroy(weapon.Value.gameObject);    
            }
        }
        _weapons.Clear();
        
        if (_unarmedWeaponInst != null && _unarmedWeaponInst.gameObject != null)
        {
            Destroy(_unarmedWeaponInst.gameObject);
        }
    }
}
