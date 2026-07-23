using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

[AbilityEffectMenu("Decorator/Delay")]
public class E_Delay : AbilityEffect
{
    [SerializeField] private int Delay;
    
    protected override async UniTask OnApply()
    {
        await UniTask.Delay(Delay);
        await ApplyChildren();
    }
}
