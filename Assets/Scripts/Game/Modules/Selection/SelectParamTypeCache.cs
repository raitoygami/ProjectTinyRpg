#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// 编辑器下缓存所有非抽象 <see cref="SelectParam"/> 派生类型（供 SerializeReference 下拉与菜单使用）。
/// </summary>
public static class SelectParamTypeCache
{
    public static IReadOnlyList<Type> ConcreteTypes => _types;

    /// <summary>与 <see cref="SelectParamKind"/> 一一对应的派生类型（用于 Kind 变更时切换 SerializeReference）。</summary>
    public static Type TypeForKind(SelectParamKind kind)
    {
        return kind switch
        {
            SelectParamKind.Sector => typeof(SelectSectorParam),
            SelectParamKind.Rect => typeof(SelectRectParam),
            SelectParamKind.Circle => typeof(SelectCircleParam),
            SelectParamKind.Point => typeof(SelectPointParam),
            _ => typeof(SelectCircleParam)
        };
    }

    static List<Type> _types = new();

    static SelectParamTypeCache()
    {
        Refresh();
    }

    [InitializeOnLoadMethod]
    static void OnDomainReload()
    {
        Refresh();
    }

    public static void Refresh()
    {
        var list = new List<Type>();
        foreach (var t in TypeCache.GetTypesDerivedFrom<SelectParam>())
        {
            if (t.IsAbstract || t.ContainsGenericParameters)
                continue;
            list.Add(t);
        }

        list.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        _types = list;
    }
}
#endif
