using Cysharp.Threading.Tasks;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    public Ability AbilityNormalAtk;
    
    public virtual void Equiped(AgentWeapon agentWeapon)
    {
        
    }
    
    public virtual async UniTask Startup(Vector2 direction, float duration)
    {
        await UniTask.NextFrame();
    }

    public virtual async UniTask Attack(Vector2 direction, float duration)
    {
        await UniTask.NextFrame();
    }

    public virtual async UniTask Recovery(float duration)
    {
        await UniTask.NextFrame();
    }
    
}
