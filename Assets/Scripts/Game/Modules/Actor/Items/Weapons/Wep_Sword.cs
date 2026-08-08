using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Wep_Sword : Weapon
{
    [SerializeField] private Transform _WepSwordFront;
    [SerializeField] private Transform _WepSwordBack;
    
    [Header("Wep Position Armed")]
    [SerializeField] private Vector3 _ArmPositionFront;
    [SerializeField] private Vector3 _ArmRotationFront;
    [SerializeField] private Vector3 _ArmPositionBack;
    [SerializeField] private Vector3 _ArmRotationBack;

    [Header("Wep Position Start up")]
    [SerializeField] private Vector3 _StartupPositionFront;
    [SerializeField] private Vector3 _StartupRotationFront;
    [SerializeField] private Vector3 _StartupPositionBack;
    [SerializeField] private Vector3 _StartupRotationBack;

    [Header("Wep Position Attack")]
    [SerializeField] private Vector3 _AttackPositionFront;
    [SerializeField] private Vector3 _AttackRotationFront;
    [SerializeField] private Vector3 _AttackPositionBack;
    [SerializeField] private Vector3 _AttackRotationBack;
    
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

    public override async UniTask Startup(Vector2 direction, float duration)
    {
        var t1 = _WepSwordFront.DOLocalMove(_StartupPositionFront, duration)
            .SetEase(Ease.InQuad);   // 可自行调整Ease
        var t2 = _WepSwordFront.DOLocalRotate(_StartupRotationFront, duration);
        
        var t3 = _WepSwordBack.DOLocalMove(_StartupPositionBack, duration)
            .SetEase(Ease.InQuad);   // 可自行调整Ease
        var t4 = _WepSwordBack.DOLocalRotate(_StartupRotationBack, duration);
        
        // 3. 等待两个动画同时完成
        await UniTask.WhenAll(
            t1.ToUniTask(),
            t2.ToUniTask(),
            t3.ToUniTask(),
            t4.ToUniTask()
        );
    }

    public override async UniTask Attack(Vector2 direction, float duration)
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();
        var tweenFront = _WepSwordFront.DOLocalMove(_AttackPositionFront, duration)
            .SetEase(Ease.OutQuad);   // 可自行调整Ease
        var tweenFrontRot = _WepSwordFront.DOLocalRotate(_AttackRotationFront, duration)
            .SetEase(Ease.OutQuad);      // 可根据需要调整Ease
            
        var tweenBack = _WepSwordBack.DOLocalMove(_AttackPositionBack, duration)
            .SetEase(Ease.OutQuad);   // 可自行调整Ease
        var tweenBackRot = _WepSwordBack.DOLocalRotate(_AttackRotationBack, duration)
            .SetEase(Ease.OutQuad);      // 可根据需要调整Ease
        // 3. 等待两个动画同时完成
        await UniTask.WhenAll(
            tweenFront.ToUniTask(cancellationToken: destroyToken),
            tweenFrontRot.ToUniTask(cancellationToken: destroyToken),
            tweenBack.ToUniTask(cancellationToken: destroyToken),
            tweenBackRot.ToUniTask(cancellationToken: destroyToken)
        );
    }

    public override async UniTask Recovery(float duration)
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();
        if (duration <= 0f)
        {
            _WepSwordFront.localPosition = _ArmPositionFront;
            _WepSwordFront.localRotation = Quaternion.Euler(_ArmRotationFront);
        
            _WepSwordBack.localPosition = _ArmPositionBack;
            _WepSwordBack.localRotation = Quaternion.Euler(_ArmRotationBack);
            return;
        }

        var tweenFront = _WepSwordFront.DOLocalMove(_ArmPositionFront, duration)
            .SetEase(Ease.OutQuad);   // 可自行调整Ease
        var tweenFrontRot = _WepSwordFront.DOLocalRotate(_ArmRotationFront, duration)
            .SetEase(Ease.OutQuad);      // 可根据需要调整Ease
            
        var tweenBack = _WepSwordBack.DOLocalMove(_ArmPositionBack, duration)
            .SetEase(Ease.OutQuad);   // 可自行调整Ease
        var tweenBackRot = _WepSwordBack.DOLocalRotate(_ArmRotationBack, duration)
            .SetEase(Ease.OutQuad);      // 可根据需要调整Ease
        // 3. 等待两个动画同时完成
        await UniTask.WhenAll(
            tweenFront.ToUniTask(cancellationToken: destroyToken),
            tweenFrontRot.ToUniTask(cancellationToken: destroyToken),
            tweenBack.ToUniTask(cancellationToken: destroyToken),
            tweenBackRot.ToUniTask(cancellationToken: destroyToken)
        );
    }
    
    private void OnDestroy()
    {
        if (_WepSwordBack.gameObject != null)
            Destroy(_WepSwordBack.gameObject);
        if (_WepSwordFront.gameObject != null)
            Destroy(_WepSwordFront.gameObject);
    }
    
}
