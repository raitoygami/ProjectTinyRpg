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
        
        _WepSwordBack.SetParent(agentWeapon.BackSlot());
        _WepSwordBack.localPosition = _ArmPositionBack;
        _WepSwordBack.localRotation = Quaternion.Euler(_ArmRotationBack);
    }

    private void OnDestroy()
    {
        Destroy(_WepSwordBack.gameObject);
        Destroy(_WepSwordFront.gameObject);
    }
}
