#if UNITY_EDITOR


using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(Dialogue))]
public class DialogueInspector : Editor {
    private SerializedProperty m_TreeRoot;
    private SerializedProperty m_Nodes;

    private ReorderableList m_NodesList;
    private ReorderableList m_Parameters;

    private RenameOverlay m_RenameOverlay;

    private void OnEnable() {
        m_RenameOverlay = new RenameOverlay();
        m_TreeRoot = serializedObject.FindProperty("TreeRoot");
        m_Nodes = serializedObject.FindProperty("Nodes");

        m_NodesList = new ReorderableList(serializedObject, m_Nodes) {
            drawElementCallback = (rect, index, _, _) => {
                EditorGUI.PropertyField(rect, m_Nodes.GetArrayElementAtIndex(index));
            },
            drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Nodes"); },
        };

        var parameters = serializedObject.FindProperty("Parameters");
        m_Parameters = new ReorderableList(serializedObject, parameters) {
            drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Parameters"); },
            onAddDropdownCallback = (_, li) => {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Bool Parameter"), false,
                    () => { OnAddParameter(li, DialogueParameterType.Bool); });

                menu.AddItem(new GUIContent("Float Parameter"), false,
                    () => { OnAddParameter(li, DialogueParameterType.Float); });

                menu.AddItem(new GUIContent("Int Parameter"), false,
                    () => { OnAddParameter(li, DialogueParameterType.Int); });

                menu.ShowAsContext();
            },
            drawElementCallback = (rect, index, _, _) => {
                var p = m_Parameters.serializedProperty.GetArrayElementAtIndex(index);
                var type = p.FindPropertyRelative("m_Type");

                switch ((DialogueParameterType) type.enumValueIndex) {
                    case DialogueParameterType.Float:
                        OnDrawParameter(rect, index, p.FindPropertyRelative("m_Name"),
                            p.FindPropertyRelative("m_DefaultFloat"));
                        break;
                    case DialogueParameterType.Bool:
                        OnDrawParameter(rect, index, p.FindPropertyRelative("m_Name"),
                            p.FindPropertyRelative("m_DefaultBool"));
                        break;
                    case DialogueParameterType.Int:
                        OnDrawParameter(rect, index, p.FindPropertyRelative("m_Name"),
                            p.FindPropertyRelative("m_DefaultInt"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            },
            elementHeight = 24
        };
    }

    public bool IsRenamingIndex(int index) {
        // Debug.Log($"{index}-{m_RenameOverlay.IsRenaming()}-{m_RenameOverlay.userData}");
        return m_RenameOverlay.IsRenaming() && m_RenameOverlay.userData == index;
    }

    private void OnDrawParameter(Rect rect, int index, SerializedProperty Name, SerializedProperty Value) {
        EditorGUI.HelpBox(rect, "", MessageType.None);

        var width = rect.width * 0.5f;
        rect.width *= 0.49f;
        rect.height = 20;
        rect.x += 3;
        rect.y += 3;

        if (IsRenamingIndex(index)) {
            if (rect.width >= 0f && rect.height >= 0f) {
                m_RenameOverlay.editFieldRect = rect;
            }

            DoRenameOverlay();
        }
        else {
            EditorGUI.LabelField(rect, Name.stringValue, EditorStyles.boldLabel);
        }

        // EditorGUI.PropertyField(rect, Name, GUIContent.none);
        rect.x += width;
        EditorGUI.PropertyField(rect, Value, GUIContent.none);
    }

    private void OnAddParameter(ReorderableList list, DialogueParameterType parameterType) {
        serializedObject.Update();

        var index = list.serializedProperty.arraySize++;
        var p = list.serializedProperty.GetArrayElementAtIndex(index);
        var type = p.FindPropertyRelative("m_Type");
        type.enumValueIndex = (int) parameterType;

        p.FindPropertyRelative("m_Name").stringValue = (DialogueParameterType) type.enumValueIndex switch {
            DialogueParameterType.Float => "New Float",
            DialogueParameterType.Bool => "New Bool",
            DialogueParameterType.Int => "New Int",
            _ => throw new ArgumentOutOfRangeException()
        };
        serializedObject.ApplyModifiedProperties();
    }

    internal static class Styles {
        public static readonly GUIContent defaultFontAssetLabel = new("Content",
            "This part can only modify by dialogue editor.");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        m_RenameOverlay.OnEvent();

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        GUILayout.Label(Styles.defaultFontAssetLabel, EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(m_TreeRoot);
        // EditorGUILayout.PropertyField(m_Nodes);
        m_NodesList.DoLayoutList();

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();

        m_Parameters.DoLayoutList();

        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties();
        }

        KeyboardHandling();

        // m_RenameOverlay.OnGUI();
    }

    private bool CanBeginRename() {
        return !m_RenameOverlay.IsRenaming() && m_Parameters.index >= 0;
    }

    private string GetNameAtIndex(int index) {
        var parameter = m_Parameters.serializedProperty.GetArrayElementAtIndex(index);
        return parameter.FindPropertyRelative("m_Name").stringValue;
    }

    public void BeginRename(int index, float delay) {
        m_RenameOverlay.BeginRename(GetNameAtIndex(index), index, delay);
        m_Parameters.index = index;
    }

    private void DoRenameOverlay() {
        if (!m_RenameOverlay.IsRenaming()) return;
        if (!m_RenameOverlay.OnGUI())
            RenameEnded();
    }

    private void RenameEnded() {
        if (m_RenameOverlay.userAcceptedRename) {
            string newName = string.IsNullOrEmpty(m_RenameOverlay.name)
                ? m_RenameOverlay.originalName
                : m_RenameOverlay.name;
            int index = m_RenameOverlay.userData;
            ChangedNameAtIndex(index, newName);
        }

        // We give keyboard focus back to our reorderable list because the rename utility stole it (now we give it back)
        if (m_RenameOverlay.HasKeyboardFocus())
            m_Parameters.GrabKeyboardFocus();

        m_RenameOverlay.Clear();

        // Debug.Log("Finish");
    }

    private void ChangedNameAtIndex(int index, string newName) {
        serializedObject.Update();
        var parameter = m_Parameters.serializedProperty.GetArrayElementAtIndex(index);
        parameter.FindPropertyRelative("m_Name").stringValue = newName;
        serializedObject.ApplyModifiedProperties();
    }

    private void KeyboardHandling() {
        var evt = Event.current;
        if (evt.type != EventType.KeyDown)
            return;

        if (m_Parameters.HasKeyboardControl()) {
            switch (Event.current.keyCode) {
                case KeyCode.F2:
                    if (CanBeginRename() && Application.platform != RuntimePlatform.OSXEditor) {
                        BeginRename(m_Parameters.index, 0f);
                        // Debug.Log("Start");
                        evt.Use();
                    }

                    break;
            }
        }
    }
}
#endif