using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     挂在 StatBar 预设上，用于显示血量与护盾比例条。通过 SetFill / SetShieldFill(0~1) 更新填充；减少时用 DOTween 做逐渐减少动画。
/// </summary>
public class UIStatBar : MonoBehaviour
{
    [SerializeField] private Image m_HpBar;
    [SerializeField] private Image m_ShieldBar;

    [Tooltip("血量减少时动画持续时间（秒）")] [SerializeField]
    private float m_DecreaseDurationHp = 0.1f;
    [Tooltip("护盾减少时动画持续时间（秒）")] [SerializeField]
    private float m_DecreaseDurationShield = 0.1f;
    private Tweener _fillTweener;
    private Tweener _shieldFillTweener;

    private void Awake()
    {
        if (m_HpBar == null)
            m_HpBar = GetComponentInChildren<Image>();
    }

    private void OnDisable()
    {
        _fillTweener?.Kill();
        _shieldFillTweener?.Kill();
        _fillTweener = null;
        _shieldFillTweener = null;
    }

    private void OnDestroy()
    {
        _fillTweener?.Kill();
        _shieldFillTweener?.Kill();
    }
    
    public void SetHpBar(float fill)
    {
        if (m_HpBar == null) return;
        var target = Mathf.Clamp01(fill);
        var current = m_HpBar.fillAmount;

        _fillTweener?.Kill();
        if (target < current)
        {
            _fillTweener = DOTween.To(
                () => m_HpBar.fillAmount,
                x =>
                {
                    m_HpBar.fillAmount = x;
                    gameObject.SetActive(x > 0);
                },
                target,
                m_DecreaseDurationHp
            ).SetEase(Ease.OutQuad).SetTarget(m_HpBar);
        }
        else
        {
            m_HpBar.fillAmount = target;
        }
    }
    
    public void SetShieldFill(float fill)
    {
        if (m_ShieldBar == null) return;
        var target = Mathf.Clamp01(fill);
        var current = m_ShieldBar.fillAmount;

        _shieldFillTweener?.Kill();
        if (target < current)
        {
            _shieldFillTweener = DOTween.To(
                () => m_ShieldBar.fillAmount,
                x => m_ShieldBar.fillAmount = x,
                target,
                m_DecreaseDurationShield
            ).SetEase(Ease.OutQuad).SetTarget(m_ShieldBar);
        }
        else
        {
            m_ShieldBar.fillAmount = target;
        }

        m_ShieldBar.gameObject.SetActive(target > 0.001f);
    }
}