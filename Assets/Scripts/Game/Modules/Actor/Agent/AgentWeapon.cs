using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AgentWeapon : MonoBehaviour
{
    [SerializeField] private Transform _weaponBack;
    [SerializeField] private Transform _weaponFront;

    public class EquipChangedEvt : EventArgs
    {
        public int Slot;
        public ItemStack Old;
        public ItemStack New;
    }

    public class EquippedWeaponChangeEvt : EventArgs
    {
        public Weapon WeaponChanged;
        public int WepAtkAbilityID;
    }

    private Weapon _unarmedWeaponInst;
    private long _currentWeaponUID = 0;
    private readonly Dictionary<long, Weapon> _weapons = new();

    public Dictionary<long, Weapon> GetWeapons()
    {
        return _weapons;
    }

    public Weapon GetWeapon(long weaponUID)
    {
        return _weapons.GetValueOrDefault(weaponUID);
    }

    public Weapon GetWeaponActive()
    {
        return _weapons.GetValueOrDefault(_currentWeaponUID, null);
    }

    public async UniTask<Weapon> InitWeapon(long weaponUID)
    {
        if (weaponUID <= 0)
            return null;
        if (_weapons.TryGetValue(weaponUID, out var weaponInst)) return weaponInst;
        
        var itemStack = PlayerManager.Instance.GetItemStackByUID(weaponUID);
        var handle = Addressables.LoadAssetAsync<GameObject>(itemStack.GetItemAddressable());
        await handle.ToUniTask();

        weaponInst = Instantiate(handle.Result, transform).GetComponent<Weapon>();
        weaponInst.gameObject.SetActive(false);
        _weapons.Add(weaponUID, weaponInst);

        return weaponInst;
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
            _unarmedWeaponInst.Equipped(this);
            await this.Publish(new EquippedWeaponChangeEvt()
                { WepAtkAbilityID = _unarmedWeaponInst.WepAtkAbilityId, WeaponChanged = _unarmedWeaponInst });
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
            await this.Publish(new EquippedWeaponChangeEvt()
                { WepAtkAbilityID = weapon.WepAtkAbilityId, WeaponChanged = weapon });
        }
        else
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(currentWeapon.GetItemAddressable());
            await handle.ToUniTask();

            var weaponInst = Instantiate(handle.Result, transform).GetComponent<Weapon>();
            weaponInst.Equipped(this);
            _weapons.Add(currentWeapon.Uid, weaponInst);
            await this.Publish(new EquippedWeaponChangeEvt()
                { WepAtkAbilityID = weaponInst.WepAtkAbilityId, WeaponChanged = weaponInst });
        }

        await UniTask.CompletedTask;
    }

    public void LoadUnarmedWeapon(Weapon unarmedWeapon)
    {
        var weapon = Instantiate(unarmedWeapon);
        _unarmedWeaponInst = weapon;
        _weapons.Add(_currentWeaponUID, weapon);
        _unarmedWeaponInst.Equipped(this);
    }
    
    public void LoadEnemyWeapon(Weapon t_Weapon)
    {
        var weapon = Instantiate(t_Weapon);
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
        foreach (var (_, weapon) in _weapons)
        {
            if (weapon.gameObject != null)
                Destroy(weapon.gameObject);
        }
        _weapons.Clear();

        if (_unarmedWeaponInst != null && _unarmedWeaponInst.gameObject != null)
        {
            Destroy(_unarmedWeaponInst.gameObject);
        }
        _unarmedWeaponInst = null;
    }
}