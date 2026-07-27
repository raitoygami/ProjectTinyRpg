using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class Wep_CrossBow : Weapon
{
    [SerializeField] private Transform _CrossBow;
    
    [SerializeField] private Vector3 _ArmPosition;
    [SerializeField] private Vector3 _ArmRotation;
    
    [SerializeField] private Vector3 _StartupPosition;

    [SerializeField] private Vector3 _RecoveryPosition;
    [SerializeField] private Vector3 _RecoveryRotation;
    
    public override void Equipped(AgentWeapon agentWeapon)
    {
        transform.SetParent(agentWeapon.transform);
        _CrossBow.SetParent(agentWeapon.FrontSlot());
        _CrossBow.localPosition = _ArmPosition;
        _CrossBow.localRotation = Quaternion.Euler(_ArmRotation);
    }

    public override async UniTask Startup(Vector2 direction, float duration)
    {
        // 1. 计算目标角度（保持原有逻辑）
        float targetAngle = Mathf.Atan2(direction.y, Mathf.Abs(direction.x)) * Mathf.Rad2Deg;
        targetAngle -= 90f;   // 默认朝 +Y 轴修正

        // 2. 获取当前旋转角度
        float currentAngle = _CrossBow.localEulerAngles.z;

        // 3. 规范化角度到 -180 ~ 180
        currentAngle = (currentAngle + 180f) % 360f - 180f;
        targetAngle = (targetAngle + 180f) % 360f - 180f;

        // 4. 计算顺时针旋转的角度差（关键修改）
        float angleDiff = targetAngle - currentAngle;

        // 强制走顺时针方向
        if (angleDiff < 0)
            angleDiff += 360f;        // 如果是逆时针，就加 360 变成顺时针

        float finalTargetAngle = currentAngle + angleDiff;

        Quaternion targetRotation = Quaternion.Euler(0f, 0f, finalTargetAngle);

        // 5. 执行动画
        var positionTween = _CrossBow.DOLocalMove(_StartupPosition, duration)
            .SetEase(Ease.InQuad);

        var rotationTween = _CrossBow.DOLocalRotateQuaternion(targetRotation, duration)
            .SetEase(Ease.InQuad);

        await UniTask.WhenAll(
            positionTween.ToUniTask(),
            rotationTween.ToUniTask()
        );

        await UniTask.Delay(200);

    }

    public override async UniTask Attack(Vector2 direction, float duration)
    {
        if (duration <= 0f)
        {
            // 瞬发情况，直接旋转30度
            Vector3 currentEuler = _CrossBow.localEulerAngles;
            currentEuler.z += 30f;                    // 逆时针旋转30度（Z轴正方向）
            _CrossBow.localRotation = Quaternion.Euler(currentEuler);
            return;
        }

        // 从当前旋转继续逆时针旋转30度
        Vector3 currentEulerAngles = _CrossBow.localEulerAngles;
        float targetZ = currentEulerAngles.z + 45f;

        Quaternion targetRotation = Quaternion.Euler(currentEulerAngles.x, currentEulerAngles.y, targetZ);

        await _CrossBow.DOLocalRotateQuaternion(targetRotation, duration)
            .SetEase(Ease.OutQuad)           // 可根据需要调整Ease
            .ToUniTask();
        
        await UniTask.Delay(200);
        
    }

    public override async UniTask Recovery(float duration)
    {
        if (duration <= 0f)
        {
            // 瞬发直接还原
            _CrossBow.localPosition = _ArmPosition;
            _CrossBow.localRotation = Quaternion.Euler(_ArmRotation);
            return;
        }

        var halfDuration = duration / 2;
        
        // 同时还原位置和旋转
        var positionTween = _CrossBow.DOLocalMove(_RecoveryPosition, halfDuration)
            .SetEase(Ease.OutQuad);

        var rotationTween = _CrossBow.DOLocalRotate(_RecoveryRotation, halfDuration)
            .SetEase(Ease.OutQuad);

        await UniTask.WhenAll(
            positionTween.ToUniTask(),
            rotationTween.ToUniTask()
        );
        
        // 同时还原位置和旋转
        var t1 = _CrossBow.DOLocalMove(_ArmPosition, halfDuration)
            .SetEase(Ease.InQuad);

        var t2 = _CrossBow.DOLocalRotate(_ArmRotation, halfDuration)
            .SetEase(Ease.InQuad);

        await UniTask.WhenAll(
            t1.ToUniTask(),
            t2.ToUniTask()
        );
        
    }
    
    private void OnDestroy()
    {
        Destroy(_CrossBow.gameObject);
    }
    
}
