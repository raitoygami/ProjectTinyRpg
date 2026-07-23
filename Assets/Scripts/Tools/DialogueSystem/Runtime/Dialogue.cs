using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// ReSharper disable All


#if UNITY_EDITOR
[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue")]
#endif
[Serializable]
public class Dialogue : ScriptableObject {
    [Tooltip("非空时：对话正常结束后会广播 DialogueCompletedEvent，供 QuestGoalDialog 等匹配。")]
    public string uid = "";

    // root entry
    public DialogueRoot TreeRoot;
    public List<DialogueEntry> Nodes = new();

    [SerializeField] public List<DialogueParameter> Parameters = new List<DialogueParameter>();

    public DialogueParameter GetParameter(string parameterName) {
        foreach (var parameter in Parameters) {
            if (parameter.Name == parameterName) {
                return parameter;
            }
        }

        return null;
    }

    public void SetFloat(string parameterName, float value) {
        var parameter = GetParameter(parameterName);
        if (parameter != null) {
            parameter.defaultFloat = value;
        }
    }

    public void SetInt(string parameterName, int value) {
        var parameter = GetParameter(parameterName);
        if (parameter != null) {
            parameter.defaultInt = value;
        }
    }

    public void SetBool(string parameterName, bool value) {
        var parameter = GetParameter(parameterName);
        if (parameter != null) {
            parameter.defaultBool = value;
        }
    }

#if UNITY_EDITOR
    public DialogueEntry CreateDialogueEntry(Type t, Vector2 location) {
        var node = CreateInstance(t) as DialogueEntry;
        if (node == null) return node;
        node.name = t.Name;
        node.guid = GUID.Generate().ToString();
        node.localtion = location;
        node.Context = this;
        Undo.RecordObject(this, "Dialogue (CreateNode)");
        Nodes.Add(node);

        AssetDatabase.AddObjectToAsset(node, this);
        Undo.RegisterCreatedObjectUndo(node, "Dialogue (CreateNode)");
        AssetDatabase.SaveAssets();

        return node;
    }

    public void DeleteNode(DialogueEntry entry) {
        Undo.RecordObject(this, "Dialogue (DeleteNode)");
        Nodes.Remove(entry);

        Undo.DestroyObjectImmediate(entry);
        AssetDatabase.SaveAssets();
    }

    public void AddChild(DialogueEntry parent, DialogueEntry child) {
        var root = parent as DialogueRoot;
        // ReSharper disable once InvertIf
        if (root) {
            Undo.RecordObject(root, "BT Add Child");
            root.Children.Add(child);
            EditorUtility.SetDirty(root);
        }

        var content = parent as DialogueLines;
        if (content) {
            Undo.RecordObject(content, "BT Add Child");
            content.Children.Add(child);
            EditorUtility.SetDirty(content);
        }

        var acceptQuest = parent as DialogueAcceptQuest;
        if (acceptQuest) {
            Undo.RecordObject(acceptQuest, "BT Add Child");
            acceptQuest.Children.Add(child);
            EditorUtility.SetDirty(acceptQuest);
        }

        var option = parent as DialogueOption;
        if (option) {
            Undo.RecordObject(option, "BT Add Child");
            option.Children.Add(child);
            EditorUtility.SetDirty(option);
        }
    }

    public void RemoveChild(DialogueEntry parent, DialogueEntry child) {
        var root = parent as DialogueRoot;
        if (root) {
            Undo.RecordObject(root, "BT Remove Child");
            root.Children.Remove(child);
            EditorUtility.SetDirty(root);
        }

        var content = parent as DialogueLines;
        if (content) {
            Undo.RecordObject(content, "BT Remove Child");
            content.Children.Remove(child);
            EditorUtility.SetDirty(content);
        }

        var acceptQuest = parent as DialogueAcceptQuest;
        if (acceptQuest) {
            Undo.RecordObject(acceptQuest, "BT Remove Child");
            acceptQuest.Children.Remove(child);
            EditorUtility.SetDirty(acceptQuest);
        }

        var option = parent as DialogueOption;
        if (option) {
            Undo.RecordObject(option, "BT Remove Child");
            option.Children.Remove(child);
            EditorUtility.SetDirty(option);
        }
    }

#endif
    public List<DialogueEntry> GetChildren(DialogueEntry parent) {
        var ret = new List<DialogueEntry>();
        var root = parent as DialogueRoot;

        if (root && root.Children != null) {
            return root.Children;
        }

        var content = parent as DialogueLines;
        if (content && content.Children != null) {
            return content.Children;
        }

        var acceptQuest = parent as DialogueAcceptQuest;
        if (acceptQuest && acceptQuest.Children != null) {
            return acceptQuest.Children;
        }

        //
        var option = parent as DialogueOption;
        return option ? option.Children : ret;
    }

#if UNITY_EDITOR
    public Dialogue Clone() {
        var tree = Instantiate(this);
        tree.TreeRoot = tree.TreeRoot.Clone() as DialogueRoot;
        return tree;
    }
#endif
}