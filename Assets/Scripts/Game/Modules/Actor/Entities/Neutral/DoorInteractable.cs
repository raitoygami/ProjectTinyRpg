using Cysharp.Threading.Tasks;
using UnityEngine;

public class DoorInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private Door _parent;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private bool _open;
    public void OnHoverEnter()
    {
        var texelSizeX = 1.0f / _spriteRenderer.sprite.texture.width;
        var texelSizeY = 1.0f / _spriteRenderer.sprite.texture.height;
        _spriteRenderer.material.SetFloat(Const.ShaderPropertyKey.TexelSizeX, texelSizeX);
        _spriteRenderer.material.SetFloat(Const.ShaderPropertyKey.TexelSizeY, texelSizeY);
        _spriteRenderer.material.SetFloat(Const.ShaderPropertyKey.OutlineThickness, 1);
    }

    public void OnHoverExit()
    {
        _spriteRenderer.material.SetFloat(Const.ShaderPropertyKey.OutlineThickness, 0);
    }

    public UniTask OnInteract()
    {
        _parent.Interact(_open);
        return UniTask.CompletedTask;
    }
}
