using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AgentAvatar : MonoBehaviour
{
    
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Mesh _generatedMesh;
    private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");

    public void SetSprite(Sprite sprite)
    {
        if (sprite == null)
            return;
        _spriteRenderer.sprite = sprite;
    } 

    public void Cover(bool cover)
    {
        /*var mat = MaterialInstance;
        if (mat != null)
            mat.SetFloat(OutlineWidth, cover ? 1 : 0);*/
    }

    private void OnDestroy()
    {
        if (_generatedMesh != null)
            Destroy(_generatedMesh);
    }

    /// <summary>
    /// 使用 Addressable 地址加载 Sprite 并设置网格显示。地址为空则不操作；加载结束后调用 <paramref name="onComplete"/>（成功或失败都会调用）。
    /// </summary>
    public void SetDisplayFromAddressable(string address, Action onComplete = null)
    {
        if (string.IsNullOrEmpty(address))
        {
            onComplete?.Invoke();
            return;
        }

        var handle = Addressables.LoadAssetAsync<Sprite>(address);
        handle.Completed += op =>
        {
            try
            {
                if (op.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    SetSprite(op.Result);
            }
            finally
            {
                onComplete?.Invoke();
            }
        };
    }
}
