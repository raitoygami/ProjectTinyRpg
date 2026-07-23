using System;
using System.Reflection;
using UnityEngine;

public class PanelBase : MonoBehaviour
{
    public virtual string PanelKey => GetType().GetCustomAttribute<PanelAttribute>()?.PanelKey ?? GetType().Name;

    public virtual bool IsOpen => gameObject.activeSelf;

    public virtual void Open() => gameObject.SetActive(true);

    public virtual void Close() => gameObject.SetActive(false);

    /// <summary>来自 <see cref="PanelAttribute.MuteGroup"/>；空表示不参与 <see cref="UIRoot"/> 的组内互斥。</summary>
    public string GetMuteGroupFromAttribute()
    {
        var attr = GetType().GetCustomAttribute<PanelAttribute>();
        return string.IsNullOrEmpty(attr?.MuteGroup) ? null : attr.MuteGroup;
    }

    public static string GetMuteGroupFromPanelType(Type panelType)
    {
        if (panelType == null || !typeof(PanelBase).IsAssignableFrom(panelType))
            return null;
        var attr = panelType.GetCustomAttribute<PanelAttribute>();
        return string.IsNullOrEmpty(attr?.MuteGroup) ? null : attr.MuteGroup;
    }
}