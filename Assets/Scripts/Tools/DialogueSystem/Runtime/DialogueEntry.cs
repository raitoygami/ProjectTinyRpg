using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class DialogueEntry : ScriptableObject {
    [SerializeField] public Dialogue Context;
    [HideInInspector] public List<DialogueEntry> Children = new();
    [HideInInspector] public string guid;
    [HideInInspector] public Vector2 localtion;
    
    public string Content;
    public int uid;
    public string Description;

    [Tooltip("非空时：仅当对应任务当前可接取时，本节点才通过条件并参与 First/All。")]
    public string questId;

    public List<DialogueCondition> Conditions = new();
    
    public void UpdateChildren()
    {
        Children.Sort((c1, c2) =>
            c1.localtion.x >= c2.localtion.x ? 1 : -1);
        foreach (var child in Children) {
            child.UpdateChildren();
        }
    }
    public virtual DialogueEntry Clone() {
        return Instantiate(this);
    }

    public virtual List<string> GetStyleClasses() {
        return null;
    }

    public virtual string GetDescription() {
        return Description;
    }

    /// <summary>本节点是否满足任务条件与参数条件（供 UI 在打开对话前等场景使用）。</summary>

    private bool CheckConditions() {
        foreach (var condition in Conditions) {
            var global = global::Context.HasInstance()
                ? global::Context.Instance.GlobalParameters
                : null;
            var parameter = global?.GetParameter(condition.Parameter)
                            ?? Context.GetParameter(condition.Parameter);
            if (parameter == null) return false;
            switch (condition.Mode) {
                case DialogueConditionMode.True:
                case DialogueConditionMode.False: {
                    return parameter.defaultBool == TrueFalseCondition(condition.Mode);
                }
                case DialogueConditionMode.Greater:
                    return GreaterCondition(condition, parameter);
                case DialogueConditionMode.Less:
                    return LessCondition(condition, parameter);
                // case DialogueConditionMode.Equals:
                //     break;
                // case DialogueConditionMode.NotEqual:
                //     break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return true;
    }

    private static bool TrueFalseCondition(DialogueConditionMode mode) {
        return mode switch {
            DialogueConditionMode.True => true,
            DialogueConditionMode.False => false,
            _ => true
        };
    }

    private static bool GreaterCondition(DialogueCondition condition, DialogueParameter parameter) {
        switch (parameter.Type) {
            case DialogueParameterType.Int when parameter.defaultInt > condition.Threshold:
            case DialogueParameterType.Float when parameter.defaultFloat > condition.Threshold:
                return true;
            case DialogueParameterType.Bool:
            default:
                return false;
        }
    }
    
    private static bool LessCondition(DialogueCondition condition, DialogueParameter parameter) {
        switch (parameter.Type) {
            case DialogueParameterType.Int when parameter.defaultInt < condition.Threshold:
            case DialogueParameterType.Float when parameter.defaultFloat < condition.Threshold:
                return true;
            case DialogueParameterType.Bool:
            default:
                return false;
        }
    }
    
    public DialogueEntry First() {
        // check all conditions
        return Children.FirstOrDefault(child => child.CheckConditions());
    }

    public List<DialogueEntry> All() {
        return Children.Where(child => child.CheckConditions()).ToList();
    }
}