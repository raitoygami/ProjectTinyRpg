using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///     挂在 StatBar 预设上，用于显示血量与护盾比例条。通过 SetFill / SetShieldFill(0~1) 更新填充；减少时用 DOTween 做逐渐减少动画。
/// </summary>
public class UIStatBar : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Image _hpBar;
    [SerializeField] private Image _shieldBar;

    [Tooltip("血量减少时动画持续时间（秒）")] [SerializeField]
    private float m_DecreaseDurationHp = 0.1f;
    [Tooltip("护盾减少时动画持续时间（秒）")] [SerializeField]
    private float m_DecreaseDurationShield = 0.1f;
    private Tweener _fillTweener;
    private Tweener _shieldFillTweener;

    private void Awake()
    {
        if (_hpBar == null)
            _hpBar = GetComponentInChildren<Image>();
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

    public void SetVisibility(float target)
    {
        _canvasGroup.alpha = Mathf.Clamp01(1 - target);
    }
    
    public void SetHpBar(float fill)
    {
        if (_hpBar == null) return;
        var target = Mathf.Clamp01(fill);
        var current = _hpBar.fillAmount;

        _fillTweener?.Kill();
        if (target < current)
        {
            _fillTweener = DOTween.To(
                () => _hpBar.fillAmount,
                x =>
                {
                    _hpBar.fillAmount = x;
                    gameObject.SetActive(x > 0);
                },
                target,
                m_DecreaseDurationHp
            ).SetEase(Ease.OutQuad).SetTarget(_hpBar);
        }
        else
        {
            _hpBar.fillAmount = target;
        }
    }
    
    public void SetShieldFill(float fill)
    {
        if (_shieldBar == null) return;
        var target = Mathf.Clamp01(fill);
        var current = _shieldBar.fillAmount;

        _shieldFillTweener?.Kill();
        if (target < current)
        {
            _shieldFillTweener = DOTween.To(
                () => _shieldBar.fillAmount,
                x => _shieldBar.fillAmount = x,
                target,
                m_DecreaseDurationShield
            ).SetEase(Ease.OutQuad).SetTarget(_shieldBar);
        }
        else
        {
            _shieldBar.fillAmount = target;
        }

        _shieldBar.gameObject.SetActive(target > 0.001f);
    }
    
}