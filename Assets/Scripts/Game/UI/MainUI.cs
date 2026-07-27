using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MainUI : MonoBehaviour
{
    [SerializeField] private Transform _currentWeaponSlot;
    
    [SerializeField] private List<Transform> _abilities = new();
    [SerializeField] private Transform _quickItemSlot1;
    [SerializeField] private Transform _quickItemSlot2;

}
