using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[Serializable]
public class RarityColorPair
{
    public ItemRarity Rarity; // 稀有度名称，如“Common”
    public Color color;      // 对应的颜色
}

[RequireComponent(typeof(RectTransform))]
public class TetrisItemNode : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _Count;
    [SerializeField] private List<RarityColorPair> _rarityColors = new List<RarityColorPair>();
    public ItemStack ItemStack { get; private set; }
    public ITetrisItemSource Owner { get; private set; }

    private Tween _sizeTween;

    public void SetOwer(ITetrisItemSource owner)
    {
        Owner = owner;
    }
    
    /// <param name="sizeTransitionDuration">自身 RectTransform 的 sizeDelta 过渡到目标尺寸的时长（秒）；≤0 表示立即完成。</param>
    public void Bind(ItemStack itemStack, ITetrisItemSource owner, float sizeTransitionDuration = 0f)
    {
        ItemStack = itemStack;
        Owner = owner;
        name = $"TetrisItemNode|{itemStack.Uid}";
        ApplySize(sizeTransitionDuration);
        Refresh();
    }
    public void Refresh()
    {
        _background.color = GetColorByRarity(ItemStack.GetRarity());
        _Count.text = ItemStack.Stackable ? ItemStack.Count.ToString() : "";
        _Count.gameObject.SetActive(ItemStack.Stackable);
    }


    public Color GetColorByRarity(ItemRarity rarity)
    {
        foreach (var pair in _rarityColors.Where(pair => pair.Rarity == rarity))
        {
            return pair.color;
        }

        return Color.white;
    }
    
    private void ApplySize(float sizeTransitionDuration)
    {
        if (Owner == null || ItemStack == null)
            return;

        var rt = GetComponent<RectTransform>();
        _sizeTween?.Kill();

        var targetSize = Owner.CalculateTetrisSize(ItemStack);

        if (sizeTransitionDuration <= 0f)
        {
            rt.sizeDelta = targetSize;
            return;
        }

        //_sizeTween = rt.DOSizeDelta(targetSize, sizeTransitionDuration);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TetrisHandle.Instance.OnNodeClicked(this, eventData);
    }

    private void OnDestroy()
    {
        _sizeTween?.Kill();
    }
}