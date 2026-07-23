using System;
using UnityEngine;

/// <summary>
/// Inspector 配置：<see cref="PanelAttribute.Root"/> 与父级 <see cref="RectTransform"/> 对应关系；
/// Addressable 实例化后的面板会挂到对应 parent 下。
/// </summary>
[Serializable]
public class UIRootPanelParentBinding
{
    [Tooltip("与面板类上 PanelAttribute.Root 一致")]
    public string rootKey;

    public RectTransform parent;
}
