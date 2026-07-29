using System;
using Cysharp.Threading.Tasks;
using JSAM;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DropItem : MonoBehaviour
{

    public class PickupItemEvt : EventArgs
    {
        public ItemStack ItemStack;
    }
    [SerializeField] private int _ItemID;
    [SerializeField] private int _Amount;

    private static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");

    [SerializeField] private SpriteRenderer spriteRenderer;

    private AsyncOperationHandle<Sprite> _iconHandle; // 关键：保存 handle

    public void Drop(int itemID, int amount)
    {
        var item = ConfigManager.Instance.GetItemBase(itemID);
        if (item == null) return;

        _ItemID = itemID;
        _Amount = amount;

        // 安全释放
        if (_iconHandle.IsValid())
        {
            Addressables.Release(_iconHandle);
            _iconHandle = default; // 可选：清空
        }

        _iconHandle = Addressables.LoadAssetAsync<Sprite>(item.Icon);

        _iconHandle.Completed += operationHandle =>
        {
            if (operationHandle.Status == AsyncOperationStatus.Succeeded)
                spriteRenderer.sprite = operationHandle.Result;
            else
                Debug.LogError($"加载图标失败: {item.Icon}");
        };
    }

    public async UniTask Pickup()
    {
        if (!PlayerManager.HasInstance())
        {
            Debug.LogError("[Loot] Pickup failed: no_inventory_manager");
            return;
        }

        var result = PlayerManager.Instance.TryAddItemStackToInventory(_ItemID, _Amount, out var itemStack);
        if (result is PlayerManager.AddItemStackToInventoryResult.SuccessNewInstance or PlayerManager.AddItemStackToInventoryResult.SuccessStacked)
        {
            AudioManager.PlaySound(GameAudioSounds.Sfx_Common_Pickup);
            await this.PublishGlobal(new PickupItemEvt(){ItemStack = itemStack});
            Destroy(gameObject);
        }
        await UniTask.CompletedTask;
    }

    public void Interactive(bool show)
    {
        spriteRenderer.material.SetFloat(OutlineThickness, show ? 1 : 0);
    }

    private void OnDestroy()
    {
        // 安全释放
        if (_iconHandle.IsValid())
        {
            Addressables.Release(_iconHandle);
            _iconHandle = default; // 可选：清空
        }
    }
}