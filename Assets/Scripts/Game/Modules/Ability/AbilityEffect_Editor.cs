#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public abstract partial class AbilityEffect
{
#if UNITY_EDITOR
    [HideInInspector] public string guid;
    [HideInInspector] public Vector2 localtion;
#endif


#if UNITY_EDITOR

    public void UpdateChildren()
    {
        Children.Sort((c1, c2) =>
            c1.localtion.x >= c2.localtion.x ? 1 : -1);
        foreach (var child in Children)
        {
            child.UpdateChildren();
        }
    }

    public virtual List<string> GetStyleClasses()
    {
        return null;
    }
#endif

#if UNITY_EDITOR
    /// <summary>编辑器创建菜单显示名：优先读 <see cref="AbilityEffectMenuAttribute"/>，否则为类名去掉 E_ 前缀。</summary>
    public static string GetClassify(Type type)
    {
        if (type == null)
        {
            return string.Empty;
        }

        var menu = type.GetCustomAttribute<AbilityEffectMenuAttribute>();
        if (menu != null && !string.IsNullOrEmpty(menu.MenuPath))
        {
            return menu.MenuPath;
        }

        return type.Name.Replace("E_", "");
    }
#endif

}

#endif