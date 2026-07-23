#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(DialogueEntry), true)]
public class DialogueEntryInspector : Editor {
    private SerializedProperty m_UID;
    private SerializedProperty m_Content;
    private SerializedProperty m_Description;
    private SerializedProperty m_Children;

    private SerializedProperty m_Conditions;
    private ReorderableList m_ConditionList;

    private void OnEnable() {
        m_UID = serializedObject.FindProperty("uid");
        m_Content = serializedObject.FindProperty("Content");
        m_Description = serializedObject.FindProperty("Description");
        m_Children = serializedObject.FindProperty("Children");
        m_Conditions = serializedObject.FindProperty("Conditions");
        m_ConditionList = new ReorderableList(serializedObject, m_Conditions) {
            drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Conditions"); },
            drawElementCallback = (rect, index, _, _) => {
                // context
                var context = serializedObject.FindProperty("Context");
                var dialogue = context.objectReferenceValue as Dialogue;
                if (dialogue == null) return;

                // condition property
                var condition = m_ConditionList.serializedProperty.GetArrayElementAtIndex(index);
                var evt = condition.FindPropertyRelative("m_ConditionEvent");
                var threshold = condition.FindPropertyRelative("m_EventTreshold");
                var conditionMode = condition.FindPropertyRelative("m_ConditionMode");

                int selectedIndex = -1;
                var options = new List<string>();

                for (int i = 0; i < dialogue.Parameters.Count; i++) {
                    var parameter = dialogue.Parameters[i];
                    if (parameter.Name == evt.stringValue) {
                        selectedIndex = i;
                    }

                    options.Add(parameter.Name);
                }


                if (selectedIndex == -1) {
                    EditorGUI.HelpBox(rect, $"Parameter {evt.stringValue} not exist in context", MessageType.Error);
                }
                else {
                    EditorGUI.HelpBox(rect, "", MessageType.None);
                    // Rect condition mode.
                    var width = rect.width;
                    rect.width = width * 0.49f;
                    rect.height = 20;
                    rect.x += 3;
                    rect.y += 3;

                    // update Event type
                    selectedIndex = EditorGUI.Popup(rect, "", selectedIndex, options.ToArray());

                    evt.stringValue = options[selectedIndex];
                    var parameter = dialogue.Parameters[selectedIndex];

                    // update condition mode & Threshold
                    rect.x += width * 0.5f;
                    switch (parameter.Type) {
                        case DialogueParameterType.Bool:
                            var enumBoolMode = conditionMode.enumNames.Where(value => value is "True" or "False")
                                .ToArray();
                            conditionMode.enumValueIndex = Mathf.Clamp(conditionMode.enumValueIndex, 0, 1);
                            conditionMode.enumValueIndex =
                                EditorGUI.Popup(rect, "", conditionMode.enumValueIndex, enumBoolMode);
                            // EditorGUI.PropertyField(rect, conditionMode);
                            break;
                        case DialogueParameterType.Float:
                        case DialogueParameterType.Int:
                            rect.width = width * 0.24f;
                            conditionMode.enumValueIndex = Mathf.Clamp(conditionMode.enumValueIndex, 2, 3);
                            // draw mode
                            var enumFloatMode = conditionMode.enumNames
                                .Where(value => value is "Greater" or "Less" or "Equals" or "NotEqual").ToArray();
                            var selectIndex =
                                EditorGUI.Popup(rect, "", conditionMode.enumValueIndex - 2, enumFloatMode);
                            conditionMode.enumValueIndex = selectIndex + 2;
                            // draw threshold
                            rect.x += width * 0.25f;

                            EditorGUI.PropertyField(rect, threshold, GUIContent.none);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            },
            onCanAddCallback = _ => {
                // context
                var context = serializedObject.FindProperty("Context");
                var dialogue = context.objectReferenceValue as Dialogue;
                return dialogue != null && dialogue.Parameters.Count > 0;
            },
            onAddCallback = list => {
                serializedObject.Update();

                var context = serializedObject.FindProperty("Context");
                var dialogue = context.objectReferenceValue as Dialogue;
                list.serializedProperty.arraySize++;
                var condition = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
                if (dialogue != null)
                    condition.FindPropertyRelative("m_ConditionEvent").stringValue = dialogue.Parameters[0].Name;

                serializedObject.ApplyModifiedProperties();
            },
            elementHeight = 25,
        };
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUI.BeginChangeCheck();
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("Context"));
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        GUILayout.Label(Styles.defaultFontAssetLabel, EditorStyles.boldLabel);
        EditorGUI.indentLevel = 1;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        EditorGUILayout.PropertyField(m_UID);
        EditorGUILayout.LabelField(Styles.defaultFontAssetLabel, EditorStyles.boldLabel);
        m_Content.stringValue = EditorGUILayout.TextArea(m_Content.stringValue, Styles.textAreaBoxWindow);
        // EditorGUILayout.PropertyField(m_Content);
        EditorGUILayout.PropertyField(m_Description);
        var questIdProp = serializedObject.FindProperty("questId");
        if (questIdProp != null)
            EditorGUILayout.PropertyField(questIdProp);
        EditorGUILayout.EndVertical();

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(m_Children);
        EditorGUI.EndDisabledGroup();

        EditorGUI.indentLevel = 0;

        EditorGUILayout.EndVertical();

        m_ConditionList.DoLayoutList();

        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties();
        }
    }

    internal static class Styles {
        public static readonly GUIContent defaultFontAssetLabel = new("Content");
        // public static readonly GUIContent defaultFontAssetLabel = new("Content");
        public static readonly GUIStyle textAreaBoxWindow = new GUIStyle(EditorStyles.textArea) { richText = true };
    }
}

#endif