using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

public partial class Ability
{

#if UNITY_EDITOR
    
    public List<AbilityEffect> GetChildren(AbilityEffect parent) {
        return parent.GetChildren();
    }
    
    public AbilityEffect CreateEffect(Type t, Vector2 location) {
        var node = CreateInstance(t) as AbilityEffect;
        if (node == null) return node;
        node.name = t.Name;
        node.guid = GUID.Generate().ToString();
        node.localtion = location;
        node.Context = this;
        Undo.RecordObject(this, "Ability (CreateEffect)");
        Effects.Add(node);

        AssetDatabase.AddObjectToAsset(node, this);
        Undo.RegisterCreatedObjectUndo(node, "Ability (CreateEffect)");
        AssetDatabase.SaveAssets();

        return node;
    }
    
    public void DeleteNode(AbilityEffect effect) {
        Undo.RecordObject(this, "Ability (DeleteEffect)");
        Effects.Remove(effect);

        Undo.DestroyObjectImmediate(effect);
        AssetDatabase.SaveAssets();
    }
    
    public void AddChild(AbilityEffect parent, AbilityEffect child) {
        //var root = parent as AbilityRoot;
        // ReSharper disable once InvertIf
        Undo.RecordObject(parent, "Ability add child.");
        parent.AddChild(child);
        EditorUtility.SetDirty(parent);

        /*var content = parent as AbilityEffectDemo;
        if (content) {
            Undo.RecordObject(content, "Ability add child.");
            content.Children.Add(child);
            EditorUtility.SetDirty(content);
        }*/
    }

    public void RemoveChild(AbilityEffect parent, AbilityEffect child) {
        Undo.RecordObject(parent, "Ability remove child.");
        parent.RemoveChild(child);
        EditorUtility.SetDirty(parent);

    }
    /*public Ability Clone() {
        var tree = Instantiate(this);
        tree.TreeRoot = tree.TreeRoot.Clone() as AbilityRoot;
        return tree;
    }*/
#endif
}
