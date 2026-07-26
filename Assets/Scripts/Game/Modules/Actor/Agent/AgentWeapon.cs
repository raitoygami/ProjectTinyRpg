using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AgentWeapon : MonoBehaviour
{
    [SerializeField] private Transform _weaponBack;
    [SerializeField] private Transform _weaponFront;

    public class WeaponChangeEvt : EventArgs
    {
        public Ability WepNormalAtk;
    }
    
    private Weapon _weaponCurrent;

    public async UniTask LoadWeapon(string addressable)
    {
        var handle = Addressables.LoadAssetAsync<GameObject>(addressable);
        handle.Completed += operationHandle =>
        {
            _weaponCurrent =  Instantiate(operationHandle.Result).GetComponent<Weapon>() ;
            _weaponCurrent.Equiped(this);
            this.Publish(new WeaponChangeEvt()
            {
                WepNormalAtk = _weaponCurrent.AbilityNormalAtk
            });
        };
        await UniTask.CompletedTask;
    }

    public void LoadWeapon(Weapon t_Weapon)
    {
        _weaponCurrent = Instantiate(t_Weapon) ;
        _weaponCurrent.Equiped(this);
    }
    
    public Weapon WeaponCurrent()
    {
        return _weaponCurrent;
    }

    public Transform FrontSlot()
    {
        return _weaponFront;
    }

    public Transform BackSlot()
    {
        return _weaponBack;
    }
    
}
