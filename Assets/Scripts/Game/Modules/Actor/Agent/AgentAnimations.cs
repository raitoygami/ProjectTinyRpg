using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class AgentAnimations : MonoBehaviour
{
    // [SerializeField][Range(0, 30f)] private float FrequenceX = 14f;
    // [SerializeField][Range(0, 30f)] private float MoveJitterFrequencyY = 19f;
    // [SerializeField][Range(0, 0.15f)] private float MoveJitterAmplitude = 0.15f;
    // [SerializeField][Range(0, 20)] private float FrequenceY = 1;
    // [SerializeField][Range(0, 1.0f)] private float AmplitudeY = 0.01f;

    private Animator animator;
    private Transform m_AnimationTarget;
    public Transform m_DiceTarget;
    public Transform[] faces = new Transform[6];

    /// <summary>3D cube face direction vectors (NOT 2D plane coordinates — do not change).</summary>
    private readonly Vector3[] localNormals = new Vector3[6];

    // private Vector3 m_BaseLocalPosition;

    // /// <summary>Idle jitter phase offsets, randomized per-unit to avoid synchronization.</summary>
    // private float _jitterPhaseX;
    // private float _jitterPhaseY;
    // private float _idleTimeOffset;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        // _jitterPhaseX = Random.Range(0f, Mathf.PI * 2f);
        // _jitterPhaseY = Random.Range(0f, Mathf.PI * 2f);
        // _idleTimeOffset = Random.Range(0f, 40f);
        this.Subscribe<AgentStats.TakeDamageEvent>(OnTakeDamage);
        localNormals[0] = new Vector3(1, 0, 0); // 右
        localNormals[1] = new Vector3(0, -1, 0); // 下
        localNormals[2] = new Vector3(0, 0, -1); // 后
        localNormals[3] = new Vector3(0, 0, 1); // 前
        localNormals[4] = new Vector3(0, 1, 0); // 上
        localNormals[5] = new Vector3(-1, 0, 0); // 左

    }

    public int GetIndex(int face)
    {
        if (face < 0 || face > 5) return 0;
        var worldDir = localNormals[face]; // 世界方向向量

        var bestFace = 0;
        var bestDot = float.NegativeInfinity;
        for (var i = 0; i <= 5; i++)
        {
            var worldNormal = m_DiceTarget.rotation * localNormals[i];
            var dot = Vector3.Dot(worldNormal, worldDir);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestFace = i;
            }
        }

        return bestFace;
    }

    public int GetUpFaceAfterMove(Vector3 moveDir)
    {
        float angle = 90f;

        // Rotation axis: Z (forward) is perpendicular to the XY plane
        Vector3 axis = Vector3.Cross(Vector3.forward, moveDir).normalized;

        Quaternion delta = Quaternion.AngleAxis(angle, axis);
        Quaternion newRotation = delta * m_DiceTarget.rotation;

        int bestIdx = 0;
        float bestDot = float.NegativeInfinity;
        for (int i = 0; i <= 5; i++)
        {
            float dot = Vector3.Dot(newRotation * localNormals[i], Vector3.up);
            if (dot > bestDot)
            {
                bestDot = dot;
                bestIdx = i;
            }
        }
        return bestIdx;
    }


    private async UniTask OnTakeDamage(AgentStats.TakeDamageEvent arg)
    {
        if (arg.Damage > 0) await TakeHit(arg.Direction, 0.1f);
    }

    private void OnDisable()
    {
        KillAllTweens();
    }

    /// <summary>
    /// Kill all tweens on m_AnimationTarget and this gameObject, even if the target has been destroyed.
    /// </summary>
    public void KillAllTweens()
    {
        if (m_AnimationTarget != null)
            m_AnimationTarget.DOKill();
        DOTween.Kill(gameObject);
    }

    public void Setup(Transform t_Target)
    {
        m_AnimationTarget = t_Target;
    }

    [Header("攻击动画参数")] public float attackMoveDistance = 0.75f;
    public float attackDuration = 2f;
    public Ease attackEase = Ease.OutQuad;

    [Header("受击动画参数")] public float hitKnockbackDistance = 0.3f;
    public float hitDuration = 1f;

    /// <summary>
    /// Attack animation (async UniTask).
    /// </summary>
    public async UniTask PunchTarget(Vector3 direction, float t_Duration)
    {
        if (m_DiceTarget  == null)
            FaceTarget(direction);

        var sr = m_AnimationTarget.GetComponentInChildren<SpriteRenderer>();
        const float forwardRatio = 0.4f;

        var originalPosition = m_AnimationTarget.position;

        var moveDir = direction.normalized;
        sr.sortingOrder += 5;
        // 1. Forward rush + scale up
        await m_AnimationTarget.DOLocalMove(moveDir * attackMoveDistance, attackDuration * forwardRatio * t_Duration)
            .SetEase(attackEase).ToUniTask();

        // 2. Bounce back
        await m_AnimationTarget.DOMove(originalPosition, attackDuration * (1 - forwardRatio) * t_Duration)
            .SetEase(Ease.OutBounce, 1.1f).ToUniTask();

        sr.sortingOrder -= 5;
    }

    /// <summary>
    /// Take-hit animation (async UniTask).
    /// </summary>
    private async UniTask TakeHit(Vector3 hitDirection, float t_Duration)
    {
        const float knockbackRatio = 0.5f;

        var originalPosition = transform.position;
        var knockDir = hitDirection.normalized;


        // 1. Flash red
        var sr = m_AnimationTarget.GetComponentInChildren<SpriteRenderer>();
        var originalColor = sr.color;
        var flashSequence = DOTween.Sequence();
        flashSequence.Append(DOTween.To(() => sr.color, x => sr.color = x, Color.red, 0.05f).SetEase(Ease.OutQuad))
            .ToUniTask().Forget();
        flashSequence.Append(DOTween.To(() => sr.color, x => sr.color = x, originalColor, 0.15f).SetEase(Ease.OutQuad))
            .ToUniTask().Forget();
        flashSequence.Play().ToUniTask().Forget();

        // 2. Knockback
        await m_AnimationTarget.DOMove(originalPosition + knockDir * hitKnockbackDistance,
                hitDuration * knockbackRatio * t_Duration)
            .SetEase(Ease.OutQuad).ToUniTask();

        // 3. Bounce back + shake
        await m_AnimationTarget.DOMove(originalPosition, hitDuration * (1 - knockbackRatio) * t_Duration)
            .SetEase(Ease.OutBounce).ToUniTask();

        await m_AnimationTarget.DOShakePosition(0.22f * t_Duration, 0.08f, 12).ToUniTask();

        await UniTask.Delay(200);
    }

    private Tween currentTween;

    /// <summary>
    /// 撞墙动画（异步版本）
    /// </summary>
    public async UniTask PlayBump(Vector3 targetWorldPos)
    {
        FaceTarget(targetWorldPos - transform.position.SnapToGrid());
        // 停止之前的动画
        currentTween?.Kill();
        
        Vector3 originalPos = m_AnimationTarget.position;
        Vector3 direction = (targetWorldPos - originalPos).normalized;
        
        // 撞击偏移距离（可调整）
        Vector3 bumpPos = originalPos + direction * 0.15f;

        // 创建 Sequence
        Sequence sequence = DOTween.Sequence();

        float bumpDuration = 0.1f;
        // 1. 快速往前撞
        _ = sequence.Append(m_AnimationTarget.DOMove(bumpPos, bumpDuration * 0.35f)
            .SetEase(Ease.OutQuad));

        // 2. 弹回 + 轻微抖动
        _ = sequence.Append(m_AnimationTarget.DOMove(originalPos, bumpDuration * 0.65f)
            .SetEase(Ease.OutBounce));

        // 3. 加入轻微位置抖动（增加撞击感）
        _ = sequence.Join(m_AnimationTarget.DOShakePosition(bumpDuration * 0.8f, 
            new Vector3(0.08f, 0.04f, 0), 
            vibrato: 14, 
            randomness: 80, 
            fadeOut: true));

        currentTween = sequence;

        // 等待动画完成
        await sequence.ToUniTask();

        m_AnimationTarget.position = originalPos;
        // 动画结束后清理
        currentTween = null;
    }
    
    
    public void FaceTarget(Vector3 t_TargetDirection)
    {
        if (m_AnimationTarget == null || Mathf.Approximately(t_TargetDirection.x, 0.0f))
            return;

        m_AnimationTarget.DOScaleX(t_TargetDirection.x > 0 ? 1 : -1, 0.1f).SetEase(Ease.Linear).SetTarget(gameObject);
    }


    public void Roll(Vector3 moveDir, float duration)
    {
        var absX = Mathf.Abs(moveDir.x);
        var absY = Mathf.Abs(moveDir.y);

        var angle = (absX + absY) * 90f;

        // Rotation axis: Z (forward) is perpendicular to the XY plane
        var axis = Vector3.Cross(Vector3.forward, moveDir).normalized;

        var startRotation = m_DiceTarget.rotation;

        var currentAngle = 0f;
        DOTween.To(() => currentAngle, x => currentAngle = x, angle, duration)
            .OnUpdate(() =>
            {
                var delta = Quaternion.AngleAxis(currentAngle, axis);
                m_DiceTarget.rotation = delta * startRotation;
            });
    }

    public void BounceOnMove(float duration)
    {
        var originY = m_AnimationTarget.localPosition.y;
        
        DOTween.To(
            () => 0f,
            t =>
            {
                var pos = m_AnimationTarget.localPosition;
                pos.y = originY + Mathf.Sin(t) * 0.25f;
                m_AnimationTarget.localPosition = pos;
            },
            Mathf.PI,
            duration
        ).SetEase(Ease.Linear).onComplete += () => { m_AnimationTarget.localPosition = Vector3.zero; };
    }

    // public void UpdateBaseAnimation(int t_Velocity)
    // {
    //     if (m_AnimationTarget == null)
    //         return;

    //     float t = Time.time;
    //     if (t_Velocity != 0)
    //     {
    //         float amp = MoveJitterAmplitude * t_Velocity;
    //         const float xBaselineRatio = 0.06f;
    //         float xOsc01 = Mathf.Sin(t * FrequenceX + _jitterPhaseX) * 0.5f + 0.5f;
    //         float jxMag = amp * xBaselineRatio + xOsc01 * amp * (0.5f - xBaselineRatio);
    //         float faceSign = Mathf.Sign(m_AnimationTarget.localScale.x);
    //         if (Mathf.Approximately(faceSign, 0f))
    //             faceSign = 1f;
    //         float jx = jxMag * faceSign;

    //         const float yBaselineRatio = 0.08f;
    //         float yOsc01 = Mathf.Sin(t * MoveJitterFrequencyY + 1.1f + _jitterPhaseY) * 0.5f + 0.5f;
    //         float jy = amp * yBaselineRatio + yOsc01 * amp * (0.55f - yBaselineRatio);
    //         m_AnimationTarget.localPosition = m_BaseLocalPosition + new Vector3(jx, jy, 0f);
    //     }
    //     else
    //         m_AnimationTarget.localPosition = m_BaseLocalPosition;

    //     var idle = 1 + Mathf.Abs(Mathf.Sin(
    //             Mathf.RoundToInt((t + _idleTimeOffset) * FrequenceY * Mathf.PI * 0.2f)
    //             * MathF.PI * 0.5f)
    //         * AmplitudeY * (1 - t_Velocity));

    //     m_AnimationTarget.localScale = new Vector3(m_AnimationTarget.localScale.x, idle, 1);
    // }
}
