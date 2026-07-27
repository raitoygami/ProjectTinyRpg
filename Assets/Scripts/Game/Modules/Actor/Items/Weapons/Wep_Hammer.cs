using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Wep_Hammer : Weapon
{
    [SerializeField] private Transform _WepSwordFront;
    
    [Header("Wep Position Armed")]
    [SerializeField] private Vector3 _ArmPositionFront;
    [SerializeField] private Vector3 _ArmRotationFront;


    [Header("Wep Position Start up")]
    [SerializeField] private Vector3 _StartupPositionFront;
    [SerializeField] private Vector3 _StartupRotationFront;

    [Header("Wep Position Attack")]
    [SerializeField] private Vector3 _AttackPositionFront;
    [SerializeField] private Vector3 _AttackRotationFront;

    public override void Equipped(AgentWeapon agentWeapon)
    {
        transform.SetParent(agentWeapon.transform);
        
        _WepSwordFront.SetParent(agentWeapon.FrontSlot());
        _WepSwordFront.localPosition = _ArmPositionFront;
        _WepSwordFront.localRotation = Quaternion.Euler(_ArmRotationFront);
        
    }

    public override async UniTask Startup(Vector2 direction, float duration)
    {
        var t1 = _WepSwordFront.DOLocalMove(_StartupPositionFront, duration)
            .SetEase(Ease.InQuad);   // 可自行调整Ease
        var t2 = _WepSwordFront.DOLocalRotate(_StartupRotationFront, duration);

        // 3. 等待两个动画同时完成
        await UniTask.WhenAll(
            t1.ToUniTask(),
            t2.ToUniTask()
        );
    }

    public override async UniTask Attack(Vector2 direction, float duration)
    {
        var tweenFront = _WepSwordFront.DOLocalMove(_AttackPositionFront, duration)
            .SetEase(Ease.OutQuad);   // 可自行调整Ease
        var tweenFrontRot = _WepSwordFront.DOLocalRotate(_AttackRotationFront, duration)
            .SetEase(Ease.OutQuad);      // 可根据需要调整Ease

        // 3. 等待两个动画同时完成
        await UniTask.WhenAll(
            tweenFront.ToUniTask(),
            tweenFrontRot.ToUniTask()
        );
    }

    public override async UniTask Recovery(float duration)
    {
        if (duration <= 0f)
        {
            _WepSwordFront.localPosition = _ArmPositionFront;
            _WepSwordFront.localRotation = Quaternion.Euler(_ArmRotationFront);
            return;
        }

        var tweenFront = _WepSwordFront.DOLocalMove(_ArmPositionFront, duration)
            .SetEase(Ease.OutQuad);   // 可自行调整Ease
        var tweenFrontRot = _WepSwordFront.DOLocalRotate(_ArmRotationFront, duration)
            .SetEase(Ease.OutQuad);      // 可根据需要调整Ease
        
        // 3. 等待两个动画同时完成
        await UniTask.WhenAll(
            tweenFront.ToUniTask(),
            tweenFrontRot.ToUniTask()
        );
    }
    
    private void OnDestroy()
    {
        Destroy(_WepSwordFront.gameObject);
    }
}
