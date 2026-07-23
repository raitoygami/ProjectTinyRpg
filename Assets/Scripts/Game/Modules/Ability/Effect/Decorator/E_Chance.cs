using Cysharp.Threading.Tasks;
using UnityEngine;

[AbilityEffectMenu("Decorator/Chance")]
public class E_Chance : AbilityEffect
{
    [SerializeField] private float Chance = 0.0f; 
    public override string GetDescription()
    {
        return $"{Chance * 100}% chance for the following effects";
    }

    protected override async UniTask OnApply()
    {
        if (Random.Range(0.0f, 1.0f) <= Chance)
        {
            await ApplyChildren();
        }
    }
    
}
