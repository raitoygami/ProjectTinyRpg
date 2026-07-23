using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(AbilityEffect), true)]
public class AbilityEffectInspector : Editor
{
    private SerializedProperty DurationBased;
    private SerializedProperty Target;
    private SerializedProperty Children;
    private ReorderableList m_ReorderableList;
    
    private void OnEnable()
    {
        DurationBased = serializedObject.FindProperty("DurationBased");
        Target = serializedObject.FindProperty("Target");
        Children = serializedObject.FindProperty("Children");
        m_ReorderableList = new ReorderableList(serializedObject, Children)
        {
            drawElementCallback = (rect, index, _, _) =>
            {
                EditorGUI.PropertyField(rect, Children.GetArrayElementAtIndex(index));
            },
            drawHeaderCallback = rect =>{EditorGUI.LabelField(rect, "Child Effects");}, 
            displayAdd = false,
            displayRemove = false
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        
        // common
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        EditorGUILayout.PropertyField(Target);
        EditorGUILayout.PropertyField(DurationBased);
        EditorGUILayout.EndVertical();

      
        var childFields = target.GetType().GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        
        foreach(var field in childFields)
        {
            if (field.IsPublic || field.GetCustomAttribute(typeof(SerializeField)) != null)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field.Name));
            }
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUI.BeginDisabledGroup(true);
        if (Children.arraySize > 0)
        {
            m_ReorderableList.DoLayoutList();    
        }
        EditorGUI.EndDisabledGroup();
        
        
        var property = serializedObject.FindProperty("Description");

        EditorGUILayout.PropertyField(property);

        
        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
