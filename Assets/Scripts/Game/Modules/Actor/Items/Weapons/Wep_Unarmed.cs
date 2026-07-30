using System;
using UnityEngine;

public class Wep_Unarmed : Weapon
{
    [SerializeField] private Transform _WepSwordFront;
    [SerializeField] private Transform _WepSwordBack;

    [Header("Wep Position Armed")]
    [SerializeField] private Vector3 _ArmPositionFront;
    [SerializeField] private Vector3 _ArmRotationFront;
    [SerializeField] private Vector3 _ArmPositionBack;
    [SerializeField] private Vector3 _ArmRotationBack;

    public override void Equipped(AgentWeapon agentWeapon)
    {
        transform.SetParent(agentWeapon.transform);
        
        _WepSwordFront.SetParent(agentWeapon.FrontSlot());
        _WepSwordFront.localPosition = _ArmPositionFront;
        _WepSwordFront.localRotation = Quaternion.Euler(_ArmRotationFront);
        _WepSwordFront.localScale = new Vector3(1f, 1f, 1f);
        
        _WepSwordBack.SetParent(agentWeapon.BackSlot());
        _WepSwordBack.localPosition = _ArmPositionBack;
        _WepSwordBack.localRotation = Quaternion.Euler(_ArmRotationBack);
        _WepSwordBack.localScale = new Vector3(1f, 1f, 1f);
        
    }

    public override void Unequip(AgentWeapon agentWeapon)
    {
        _WepSwordFront.SetParent(transform);
        _WepSwordBack.SetParent(transform);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_WepSwordBack.gameObject != null)
            Destroy(_WepSwordBack.gameObject); 

        if (_WepSwordFront.gameObject != null)
            Destroy(_WepSwordFront.gameObject);
    }
}
