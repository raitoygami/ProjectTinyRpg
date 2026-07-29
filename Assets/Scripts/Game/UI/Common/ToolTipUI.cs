using System;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class ToolTipUI : MonoBehaviour
{
    [SerializeField] private Image _Icon;
    [SerializeField] private TMP_Text _Name;
    [SerializeField] private TMP_Text _Category;

    [SerializeField] private RectTransform _transform;

    private AsyncOperationHandle<Sprite> _iconHandle; // 关键：保存 handle

    private ItemStack _itemStack;

    private void Awake()
    {
        _transform.gameObject.SetActive(false);
    }

    public void ShowTip(ItemStack itemStack, Vector3 anchoredPosition)
    {
        if (_itemStack != itemStack)
        {
            // 安全释放
            if (_iconHandle.IsValid())
            {
                Addressables.Release(_iconHandle);
                _iconHandle = default; // 可选：清空
            }

            // 图标
            _iconHandle = Addressables.LoadAssetAsync<Sprite>(itemStack.GetIconAddressable());

            _iconHandle.Completed += operationHandle =>
            {
                if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                    _Icon.sprite = operationHandle.Result;
                else
                    Debug.LogError($"加载图标失败: {itemStack.GetIconAddressable()}");
            };
        }

        _Name.text = itemStack.Name();
        _Category.text = itemStack.Category();
        
        _transform.gameObject.SetActive(true);
        _transform.anchoredPosition = anchoredPosition;
        
        _itemStack = itemStack;
    }

    public void HideTip()
    {
        _transform.gameObject.SetActive(false);
    }
    
}
