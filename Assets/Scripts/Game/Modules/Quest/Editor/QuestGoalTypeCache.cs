#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// 编辑器下缓存所有非抽象 <see cref="QuestGoal"/> 派生类型（供 Phase 的 Goals 列表 + 菜单使用）。
/// </summary>
public static class QuestGoalTypeCache
{
    public static IReadOnlyList<Type> ConcreteQuestGoalTypes => _types;

    static List<Type> _types = new();

    static QuestGoalTypeCache()
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
        foreach (var t in TypeCache.GetTypesDerivedFrom<QuestGoal>())
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
