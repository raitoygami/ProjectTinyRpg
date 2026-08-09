using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IInteractable
{
    // 鼠标悬停进入（用于高亮）
    void OnHoverEnter();
    
    // 鼠标悬停离开（取消高亮）
    void OnHoverExit();
    
    // 点击时触发的动作
    UniTask OnInteract();
}