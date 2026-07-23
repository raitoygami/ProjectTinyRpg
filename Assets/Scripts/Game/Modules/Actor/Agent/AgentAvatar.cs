using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AgentAvatar : MonoBehaviour
{
    [SerializeField] private OutfitSpriteSet outfitTest;

    [Tooltip("为 true 时将 sprite 的主纹理赋给 Renderer.material 实例；为 false 则不改写 mainTexture（如共用图集材质）")]
    [SerializeField] private bool _instantiateMaterialWithSpriteTexture = true;

    [SerializeField] private MeshFilter _meshFilter;
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private MeshCollider _meshCollider;

    [SerializeField] private SpriteRenderer _spriteRenderer;

    private Mesh _generatedMesh;
    private static readonly int OutlineWidth = Shader.PropertyToID("_OutlineWidth");

    /// <summary>获取本 Renderer 的材质实例（首次访问会由 Unity 从 sharedMaterial 拷贝一份）。</summary>
    private Material MaterialInstance =>
        _meshRenderer != null ? _meshRenderer.material : null;

    /// <summary>使用当前 Renderer 上的材质实例，仅更新主贴图为 sprite 所在纹理（不重建网格）。</summary>
    public void RefreshMaterialTexture(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null || _meshRenderer == null) return;
        if (!_instantiateMaterialWithSpriteTexture) return;

        MaterialInstance.mainTexture = sprite.texture;
    }

    /// <summary>根据 Sprite 创建/替换网格并渲染；材质通过 <see cref="MeshRenderer.material"/> 实例化后绑定贴图。</summary>
    /// <param name="sprite">不可为 null；为 null 时调用 <see cref="ClearGeneratedMesh"/>。</param>
    // public void SetSprite(Sprite sprite)
    // {
    //     if (sprite == null)
    //     {
    //         ClearGeneratedMesh();
    //         return;
    //     }

    //     if (_generatedMesh != null)
    //     {
    //         Destroy(_generatedMesh);
    //         _generatedMesh = null;
    //     }

    //     _generatedMesh = SpriteMeshGenerator.CreateMeshFromSpriteGeometry(sprite);

    //     if (_generatedMesh == null)
    //     {
    //         Debug.LogWarning($"EntityRenderer: 无法为 Sprite '{sprite.name}' 生成网格（请检查 Sprite 是否可读写或物理形状）。", this);
    //         return;
    //     }

    //     _meshFilter.sharedMesh = _generatedMesh;
    //     if (_meshCollider != null)
    //         _meshCollider.sharedMesh = _generatedMesh;

    //     ApplyMaterialForSprite(sprite);
    // }

    public void SetSprite(Sprite sprite)
    {
        if (sprite == null)
            return;
        _spriteRenderer.sprite = sprite;
    } 

    private void ApplyMaterialForSprite(Sprite sprite)
    {
        if (_meshRenderer == null) return;

        if (_instantiateMaterialWithSpriteTexture && sprite != null && sprite.texture != null)
            MaterialInstance.mainTexture = sprite.texture;
    }

    public void Cover(bool cover)
    {
        var mat = MaterialInstance;
        if (mat != null)
            mat.SetFloat(OutlineWidth, cover ? 1 : 0);
    }

    /// <summary>移除生成的网格（不销毁组件）。</summary>
    public void ClearGeneratedMesh()
    {
        if (_generatedMesh != null)
        {
            Destroy(_generatedMesh);
            _generatedMesh = null;
        }

        if (_meshFilter != null)
            _meshFilter.sharedMesh = null;
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

    public void UpdateOutfit()
    {
        var pixelsPerUnit = 80;
        var outfit =
            OutfitComposer.Compose(outfitTest, pixelsPerUnit, new Vector2(0.5f, 0), OutfitComposeMode.GpuBlit);
        SetSprite(outfit);
    }
}
