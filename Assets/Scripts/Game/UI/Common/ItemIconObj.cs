using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class ItemIconObj : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _amount;
    private AsyncOperationHandle<Sprite> _iconHandle; // 关键：保存 handle

    private ItemStack _itemStack;
    public void SetItemStack(ItemStack itemStack)
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
                    _icon.sprite = operationHandle.Result;
                else
                    Debug.LogError($"加载图标失败: {itemStack.GetIconAddressable()}");
            };
        }

        // 数量
        _amount.text = itemStack.Count.ToString();
        _amount.gameObject.SetActive(!itemStack.Stackable);

        _itemStack = itemStack;
    }
}
