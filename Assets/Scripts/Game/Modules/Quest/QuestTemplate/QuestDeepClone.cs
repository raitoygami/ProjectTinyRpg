using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// 从 <see cref="QuestTemplateAsset"/> 等资源深拷贝 <see cref="Quest"/> 图（多态 <see cref="QuestGoal"/>），与 <see cref="PersistenceModule"/> 的存档 JSON 无关。
/// </summary>
public static class QuestDeepClone
{
    public static Quest CloneQuest(Quest source)
    {
        if (source == null) return null;

        var q = new Quest
        {
            questId = source.questId,
            type = source.type,
            repeatable = source.repeatable,
            currentPhaseIndex = source.currentPhaseIndex,
            acceptedMinutes = source.acceptedMinutes,
            completedMinutes = source.completedMinutes,
            autoBindPubSubGoalListeners = source.autoBindPubSubGoalListeners,
        };

        q.phases = new List<QuestPhase>();
        if (source.phases != null)
        {
            foreach (var p in source.phases)
                q.phases.Add(ClonePhase(p));
        }

        return q;
    }

    static QuestPhase ClonePhase(QuestPhase p)
    {
        if (p == null) return null;

        var phase = new QuestPhase { phaseId = p.phaseId ?? "" };
        phase.goals = new List<QuestGoal>();
        if (p.goals == null) return phase;

        foreach (var g in p.goals)
        {
            var cg = CloneGoal(g);
            if (cg != null)
                phase.goals.Add(cg);
        }

        return phase;
    }

    static QuestGoal CloneGoal(QuestGoal g)
    {
        if (g == null) return null;

        var type = g.GetType();
        if (type.IsAbstract) return null;

        QuestGoal clone;
        try
        {
            clone = (QuestGoal)Activator.CreateInstance(type);
        }
        catch (Exception e)
        {
            Debug.LogError($"[QuestDeepClone] 无法构造 {type.Name}（需要公共无参构造函数）：{e.Message}");
            return null;
        }

        CopyGoalFieldsRecursive(g, clone, type);
        return clone;
    }

    const BindingFlags FieldFlags =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    static void CopyGoalFieldsRecursive(QuestGoal src, QuestGoal dst, Type concreteType)
    {
        for (Type t = concreteType; t != null && t != typeof(object); t = t.BaseType)
        {
            if (!typeof(QuestGoal).IsAssignableFrom(t))
                break;

            foreach (var f in t.GetFields(FieldFlags))
            {
                if (f.IsStatic) continue;
                if (Attribute.IsDefined(f, typeof(NonSerializedAttribute), inherit: false))
                    continue;
                f.SetValue(dst, f.GetValue(src));
            }
        }
    }
}
