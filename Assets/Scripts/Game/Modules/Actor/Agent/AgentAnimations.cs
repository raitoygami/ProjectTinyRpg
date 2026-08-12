using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using JSAM;
using UnityEngine;

public class AgentAnimations : MonoBehaviour
{
    [Header("攻击动画参数")] public float attackMoveDistance = 0.75f;
    public float attackDuration = 1f;
    public Ease attackEase = Ease.OutQuad;

    [Header("受击动画参数")] public float hitKnockbackDistance = 0.3f;
    public float hitDuration = 1f;

    [SerializeField] [Range(0, 20)] private float FrequenceX = 10f;
    [SerializeField] [Range(0, 10.0f)] private float AmplitudeX = 5.0f;
    [SerializeField] [Range(0, 20)] private float FrequenceY = 3;
    [SerializeField] [Range(0, 1.0f)] private float AmplitudeY = 0.1f;

    private float _jitterPhaseX;
    private float _jitterPhaseY;
    private float _idleTimeOffset;

    private Transform _avatarTarget;
    private Transform m_SpriteRoot;

    private void Awake()
    {
        this.Subscribe<AgentStats.TakeDamageEvent>(OnTakeDamage);
    }

    private async UniTask OnTakeDamage(AgentStats.TakeDamageEvent arg)
    {
        if (arg.Damage > 0) await TakeHit(arg.Direction, 0.1f);
    }

    private void OnDisable()
    {
        KillAllTween();
    }

    /// <summary>
    ///     Kill all tweens on m_AnimationTarget and this gameObject, even if the target has been destroyed.
    /// </summary>
    public void KillAllTween()
    {
        if (_avatarTarget != null)
            _avatarTarget.DOKill();
        if (m_SpriteRoot != null)
            m_SpriteRoot.DOKill();
        DOTween.Kill(gameObject);
    }

    public void Setup(Transform t_AvatarRoot, Transform t_SpriteRoot)
    {
        _avatarTarget = t_AvatarRoot;
        m_SpriteRoot = t_SpriteRoot;
    }

    /// <summary>
    ///     Attack animation (async UniTask).
    /// </summary>
    public async UniTask PunchTarget(Vector3 direction, float t_Duration, GameAudioSounds t_Sound)
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();

        FaceTarget(direction);

        const float windupRatio = 1.0f; // 前摇占 attackDuration * t_Duration 的比例
        const float forwardRatio = 0.25f;

        var windupDistance = attackMoveDistance * 0.5f; // 后撤距离，可根据需要调整

        var originalPosition = _avatarTarget.localPosition;

        var moveDir = direction.normalized;

        // 0. 前摇：向反方向移动一小段，模拟蓄力
        await _avatarTarget.DOLocalMove(-moveDir * windupDistance, attackDuration * windupRatio * t_Duration)
            .SetEase(Ease.InQuad)
            .ToUniTask(cancellationToken: destroyToken);

        AudioManager.PlaySound(t_Sound);

        // 1. Forward rush + scale up
        await _avatarTarget.DOLocalMove(moveDir * attackMoveDistance, attackDuration * forwardRatio * t_Duration)
            .SetEase(attackEase).ToUniTask(cancellationToken: destroyToken);

        // 2. Bounce back
        _avatarTarget.DOLocalMove(originalPosition, attackDuration * (1 - forwardRatio) * t_Duration)
            .SetEase(Ease.OutBounce, 1.1f).ToUniTask(cancellationToken: destroyToken).Forget();
    }

    /// <summary>
    ///     Attack animation (async UniTask).
    /// </summary>
    public async UniTask SwordSlash(Vector3 direction, float t_Duration, GameAudioSounds t_Sound)
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();
        FaceTarget(direction);

        const float windupRatio = 1.0f; // 前摇占 attackDuration * t_Duration 的比例
        const float forwardRatio = 0.25f;

        var windupDistance = attackMoveDistance * 0.5f; // 后撤距离，可根据需要调整

        var originalPosition = _avatarTarget.localPosition;

        var moveDir = direction.normalized;

        var agentWeapon = GetComponent<AgentWeapon>();
        var weapon = agentWeapon.WeaponCurrent();

        // 0. 前摇：向反方向移动一小段，模拟蓄力
        var startupDuration = attackDuration * windupRatio * t_Duration;
        var t1 = _avatarTarget.DOLocalMove(-moveDir * windupDistance, startupDuration)
            .SetEase(Ease.InQuad)
            .ToUniTask(cancellationToken: destroyToken);
        var t2 = weapon.Startup(moveDir, startupDuration);
        await UniTask.WhenAll(t1, t2);

        await UniTask.Delay((int)(t_Duration * 1000), cancellationToken: destroyToken);

        AudioManager.PlaySound(t_Sound);

        var slashDuration = attackDuration * forwardRatio * t_Duration;
        // 1. Forward rush + scale up
        t1 = _avatarTarget.DOLocalMove(moveDir * attackMoveDistance, attackDuration * forwardRatio * t_Duration)
            .SetEase(Ease.OutQuad).ToUniTask(cancellationToken: destroyToken);
        t2 = weapon.Attack(Vector2.zero, slashDuration);
        await UniTask.WhenAll(t1, t2);

        // 2. Bounce back
        var recoveryDuration = attackDuration * (1 - forwardRatio) * t_Duration;
        t1 = _avatarTarget.DOLocalMove(originalPosition, recoveryDuration)
            .SetEase(Ease.OutBounce, 1.1f).ToUniTask(cancellationToken: destroyToken);
        t2 = weapon.Recovery(recoveryDuration);

        UniTask.WhenAll(t1, t2).Forget();
    }

    public async UniTask BowShot(Vector3 direction, float t_Duration, GameAudioSounds t_Sound)
    {
        FaceTarget(direction);

        const float startupRatio = 0.25f; // 前摇占比（拉弓时间较长）

        var originalPosition = _avatarTarget.localPosition;
        var moveDir = direction.normalized;

        // 播放射箭音效
        AudioManager.PlaySound(t_Sound);

        // ==================== 1. 前摇 - 拉弓 (武器动画) ====================
        // 角色不动，只让武器做前摇
        var agentWeapon = GetComponent<AgentWeapon>();
        var weapon = agentWeapon.WeaponCurrent();
        if (weapon != null)
            await agentWeapon.WeaponCurrent().Startup(moveDir, attackDuration * startupRatio * t_Duration);

        // ==================== 后续动画全部 Fire-and-Forget，但保持顺序 ====================
        RunBowAnimationSequence(moveDir, originalPosition, weapon, t_Duration).Forget();
    }

    // 内部私有方法，负责后续动画序列
    private async UniTask RunBowAnimationSequence(Vector3 moveDir, Vector3 originalPosition, Weapon weapon,
        float t_Duration)
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();
        const float attackRatio = 0.5f; // 射箭瞬间
        const float recoveryRatio = 0.25f; // 后摇占比

        // 2. 攻击瞬间 - 后坐力 + 武器攻击
        var recoilDistance = attackMoveDistance * 0.5f;
        var durationA = attackDuration * attackRatio * t_Duration;
        var durationB = durationA * 0.5f; // 你之前想要 b 是 a 的一半

        var recoilTask = _avatarTarget.DOLocalMove(-moveDir * recoilDistance, durationA)
            .SetEase(Ease.OutQuad)
            .ToUniTask(cancellationToken: destroyToken);

        // a 播到一半时开始 b
        await UniTask.Delay((int)(durationA * 0.5f * 1000));

        var weaponAttackTask = weapon != null
            ? weapon.Attack(moveDir, durationB)
            : UniTask.CompletedTask;

        await UniTask.WhenAll(recoilTask, weaponAttackTask);

        // 3. 后摇 - 复位 + 武器收弓
        var returnTask = _avatarTarget.DOLocalMove(originalPosition,
                attackDuration * recoveryRatio * t_Duration)
            .SetEase(Ease.OutQuad)
            .ToUniTask(cancellationToken: destroyToken);

        var weaponRecoveryTask = weapon != null
            ? weapon.Recovery(attackDuration * recoveryRatio * t_Duration)
            : UniTask.CompletedTask;

        await UniTask.WhenAll(returnTask, weaponRecoveryTask);
    }

    private async UniTask TakeHit(Vector3 hitDirection, float t_Duration)
    {
        const float knockbackRatio = 0.5f;

        // 1. 获取销毁取消令牌（如果对象被 Destroy，此令牌会被触发）
        var destroyToken = this.GetCancellationTokenOnDestroy();

        var originalPosition = Vector3.zero;
        var knockDir = hitDirection.normalized;

        AudioManager.PlaySound(GameAudioSounds.Sfx_Combat_Hit);

        // 2. 获取 SpriteRenderer
        var sr = _avatarTarget.GetComponentInChildren<SpriteRenderer>();
        var originalColor = sr.color;

        // 3. 制作闪光动画 - 关键修改：用 Await 代替 Forget，并传入取消令牌
        try
        {
            var flashSequence = DOTween.Sequence();
            _ = flashSequence.Append(DOTween.To(() => sr.color, x => sr.color = x, Color.red, 0.05f)
                .SetEase(Ease.OutQuad));
            _ = flashSequence.Append(DOTween.To(() => sr.color, x => sr.color = x, originalColor, 0.15f)
                .SetEase(Ease.OutQuad));

            // ✅ 使用 WithCancellation 并 SuppressCancellationThrow（避免抛出异常导致报错）
            // 如果对象销毁，这里会立即结束，不会报错
            await flashSequence.Play().ToUniTask(cancellationToken: destroyToken)
                .SuppressCancellationThrow();
        }
        catch (OperationCanceledException)
        {
            // 被取消时（对象销毁），直接终止函数
            return;
        }

        // 4. 如果在等待闪光期间对象已销毁，立刻退出
        if (destroyToken.IsCancellationRequested || this == null || _avatarTarget == null)
            return;

        // 5. 击退 + 回弹 + 震动（统一传入取消令牌）
        await _avatarTarget.DOLocalMove(originalPosition + knockDir * hitKnockbackDistance,
                hitDuration * knockbackRatio * t_Duration)
            .SetEase(Ease.OutQuad)
            .ToUniTask(cancellationToken: destroyToken)
            .SuppressCancellationThrow();

        // 如果在击退期间被销毁，退出
        if (destroyToken.IsCancellationRequested || this == null || _avatarTarget == null)
            return;

        await _avatarTarget.DOLocalMove(originalPosition, hitDuration * (1 - knockbackRatio) * t_Duration)
            .SetEase(Ease.OutBounce)
            .ToUniTask(cancellationToken: destroyToken)
            .SuppressCancellationThrow();

        if (destroyToken.IsCancellationRequested || this == null || _avatarTarget == null)
            return;

        await _avatarTarget.DOShakePosition(0.22f * t_Duration, 0.08f, 12)
            .ToUniTask(cancellationToken: destroyToken)
            .SuppressCancellationThrow();
    }

    private Tween currentTween;

    /// <summary>
    ///     撞墙动画（异步版本）
    /// </summary>
    public async UniTask PlayBump(Vector3 targetWorldPos)
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();

        FaceTarget(targetWorldPos - transform.position.SnapToGrid());
        // 停止之前的动画
        currentTween?.Kill();

        // 播放音效 denied
        AudioManager.PlaySound(GameAudioSounds.Sfx_Common_Denied);

        var originalPos = _avatarTarget.position;
        var direction = (targetWorldPos - originalPos).normalized;

        // 撞击偏移距离（可调整）
        var bumpPos = originalPos + direction * 0.15f;

        // 创建 Sequence
        var sequence = DOTween.Sequence();

        var bumpDuration = 0.25f;
        // 1. 快速往前撞
        _ = sequence.Append(_avatarTarget.DOMove(bumpPos, bumpDuration * 0.35f)
            .SetEase(Ease.OutQuad));

        // 2. 弹回 + 轻微抖动
        _ = sequence.Append(_avatarTarget.DOMove(originalPos, bumpDuration * 0.65f)
            .SetEase(Ease.OutBounce));

        // 3. 加入轻微位置抖动（增加撞击感）
        _ = sequence.Join(_avatarTarget.DOShakePosition(bumpDuration * 0.8f,
            new Vector3(0.08f, 0.04f, 0),
            14,
            80,
            fadeOut: true));

        currentTween = sequence;

        // 等待动画完成
        await sequence.ToUniTask(cancellationToken: destroyToken);

        _avatarTarget.position = originalPos;
        // 动画结束后清理
        currentTween = null;
    }

    public async UniTask Death()
    {
        var destroyToken = this.GetCancellationTokenOnDestroy();
        var renderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        var tasks = new List<UniTask>();

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var sr in renderers)
        {
            // 检查材质是否支持该属性（通过 Material 缓存判断）
            var mat = sr.material;
            if (mat == null || !mat.HasProperty(Const.ShaderPropertyKey.DissolveClip))
                continue;
            // 获取初始值（一般是0）

            var tween = DOTween.To(
                () => mat.GetFloat(Const.ShaderPropertyKey.DissolveClip),
                x => { mat.SetFloat(Const.ShaderPropertyKey.DissolveClip, x); },
                1,
                1
            );

            tasks.Add(tween.ToUniTask(cancellationToken: destroyToken)
                .SuppressCancellationThrow());
        }

        await UniTask.WhenAll(tasks);
    }


    private bool _visible = true;

    public void RefreshVisibility()
    {
        SetVisibility(_visible);
    }
    
    public void SetVisibility(bool visible)
    {
        var renderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            var mat = sr.material;
            if (mat == null || !mat.HasProperty(Const.ShaderPropertyKey.Fade))
                continue;
            
            mat.SetFloat(Const.ShaderPropertyKey.Fade, visible ? 0 : 1);
        }
        _visibilityChangedEvent.Fade = visible ? 0 : 1;
        _visible = visible;
        this.Publish(_visibilityChangedEvent);
    }

    public class VisibilityChangedEvent : EventArgs
    {
        public float Fade;
    }

    private readonly VisibilityChangedEvent _visibilityChangedEvent = new();

    private CancellationTokenSource _fadeCts;
    private TweenerCore<float, float, FloatOptions> _tweenFadeout;
    private TweenerCore<float, float, FloatOptions> _tweenFadein;

    public async UniTask Fadeout()
    {
        if (!_visible)
            return;
        _visible = false;
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = new CancellationTokenSource();

        // 合并对象销毁令牌与当前自定义令牌
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy(),
            _fadeCts.Token
        );
        var token = linkedCts.Token;
        var renderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        
        var progress = 0f;
        _tweenFadeout = DOTween.To(
            () => progress,
            x => progress = x,
            1f,
            0.25f
        ).OnUpdate(() =>
        {
            // 在每一帧更新时，遍历所有材质，计算当前目标值并应用
            foreach (var sr in renderers)
            {
                var mat = sr.material;
                if (mat == null || !mat.HasProperty(Const.ShaderPropertyKey.Fade))
                    continue;
                mat.SetFloat(Const.ShaderPropertyKey.Fade, progress);
                // 发布事件（与原逻辑一致，每个材质更新时发布一次）
            }
            _visibilityChangedEvent.Fade = progress;
            this.Publish(_visibilityChangedEvent);
        });

// 将 Tween 转换为 UniTask，支持取消，并抑制取消抛出的异常
        await _tweenFadeout.ToUniTask(cancellationToken: token)
            .SuppressCancellationThrow();
    }

    public async UniTask Fadein()
    {
        if (_visible)
            return;
        _visible = true;

        // 同样的逻辑，只是目标值改为 0
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = new CancellationTokenSource();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            this.GetCancellationTokenOnDestroy(),
            _fadeCts.Token
        );
        var token = linkedCts.Token;
        var renderers = gameObject.GetComponentsInChildren<SpriteRenderer>();
        
        var progress = 1f;
        _tweenFadeout = DOTween.To(
            () => progress,
            x => progress = x,
            0f,
            0.25f
        ).OnUpdate(() =>
        {
            // 在每一帧更新时，遍历所有材质，计算当前目标值并应用
            foreach (var sr in renderers)
            {
                var mat = sr.material;
                if (mat == null || !mat.HasProperty(Const.ShaderPropertyKey.Fade))
                    continue;
                mat.SetFloat(Const.ShaderPropertyKey.Fade, progress);
                // 发布事件（与原逻辑一致，每个材质更新时发布一次）
       
            }
            _visibilityChangedEvent.Fade = progress;
            this.Publish(_visibilityChangedEvent);
        });

// 将 Tween 转换为 UniTask，支持取消，并抑制取消抛出的异常
        await _tweenFadeout.ToUniTask(cancellationToken: token)
            .SuppressCancellationThrow();
    }


    public Vector3 GetDirection()
    {
        return _avatarTarget.localScale;
    }

    public void SetDirection(Vector3 direction)
    {
        _avatarTarget.localScale = direction;
    }

    public void FaceTarget(Vector3 t_TargetDirection)
    {
        if (_avatarTarget == null || Mathf.Approximately(t_TargetDirection.x, 0.0f))
            return;

        _avatarTarget.DOScaleX(t_TargetDirection.x > 0 ? 1 : -1, 0.1f).SetEase(Ease.Linear).SetTarget(gameObject);
    }

    public void BounceOnMove(float duration)
    {
        var originY = _avatarTarget.localPosition.y;

        DOTween.To(
            () => 0f,
            t =>
            {
                if (_avatarTarget.gameObject == null) return;
                var pos = _avatarTarget.localPosition;
                pos.y = originY + Mathf.Sin(t) * 0.125f;
                _avatarTarget.localPosition = pos;
            },
            Mathf.PI,
            duration
        ).SetEase(Ease.Linear).onComplete += () =>
        {
            if (_avatarTarget.gameObject == null) return;
            _avatarTarget.localPosition = Vector3.zero;
        };
    }

    public void UpdateBaseAnimation(int t_Velocity)
    {
        if (m_SpriteRoot == null)
            return;
        var t = Time.unscaledTime;

        var beatDuration = 60f / CombatManager.CombatMusicDPM;
        var frequency = FrequenceY / beatDuration;
        var amplitude = AmplitudeY * CombatManager.CombatMusicMul;
        var idle = 1 - Mathf.Abs(Mathf.Sin(
                                     Mathf.RoundToInt((t + _idleTimeOffset) * frequency))
                                 * amplitude * (1 - t_Velocity));

        var moveAnimation = Quaternion.Euler(0, 0,
            Mathf.Sin(Mathf.Floor(Time.time * FrequenceX * t_Velocity * Mathf.PI)) * AmplitudeX);
        m_SpriteRoot.rotation = moveAnimation;
        m_SpriteRoot.transform.localScale = new Vector3(m_SpriteRoot.transform.localScale.x, idle, 1);
    }

    private void OnDestroy()
    {
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _tweenFadeout.Kill();
        _tweenFadeout = null;
        _tweenFadein.Kill();
        _tweenFadein = null;
        KillAllTween();
    }
}