#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

[CustomEditor(typeof(Ability))]
public class AbilityInspector : Editor
{
    private SerializedProperty m_Name;
    private SerializedProperty m_Icon;
    private SerializedProperty m_WeaponTypeRequire;
    private SerializedProperty m_Desc;
    private SerializedProperty m_Range;
    private SerializedProperty m_SkillDisplayParam;
    private SerializedProperty m_TargetMode;
    private SerializedProperty _cooldown;

    private SerializedProperty costMP;
    private SerializedProperty costHP;
    
    private SerializedProperty m_TreeRoot;
    private ReorderableList m_ReorderableList;

    private void OnEnable()
    {
        m_Name = serializedObject.FindProperty("AbilityName");
        m_Icon = serializedObject.FindProperty("Icon");
        //m_Desc = serializedObject.FindProperty("");
        m_WeaponTypeRequire = serializedObject.FindProperty("WeaponTypeRequire");
        m_Range = serializedObject.FindProperty("m_Range");
        m_SkillDisplayParam = serializedObject.FindProperty("m_SkillDisplayParam");
        m_TargetMode = serializedObject.FindProperty("m_TargetMode");
        _cooldown = serializedObject.FindProperty("Cooldown");
        
        costHP = serializedObject.FindProperty("CostHP");
        costMP = serializedObject.FindProperty("CostMP");
        
        m_TreeRoot = serializedObject.FindProperty("TreeRoot");
        var effects = serializedObject.FindProperty("Effects");
        
        // ReSharper disable once UseObjectOrCollectionInitializer
        m_ReorderableList = new ReorderableList(serializedObject, effects);
        m_ReorderableList.drawHeaderCallback = DrawHeader;
        m_ReorderableList.drawElementCallback = (rect, index, _, _) =>
        {
            EditorGUI.PropertyField(rect, effects.GetArrayElementAtIndex(index));
        };
        m_ReorderableList.displayAdd = false;
        m_ReorderableList.displayRemove = false;
    }
    private static void DrawHeader(Rect rect)
    {
        GUI.Label(rect, "");
    }
    
    internal static class Styles {
        public static readonly GUIContent Cost = new("Cost",
            "");
        public static readonly GUIContent defaultFontAssetLabel = new("Content",
            "This part can only modify by ability editor.");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();
        // properties
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        
        EditorGUILayout.PropertyField(m_Name);
        EditorGUILayout.PropertyField(m_Icon);
        EditorGUILayout.PropertyField(m_WeaponTypeRequire);
        EditorGUILayout.PropertyField(m_Range);
        EditorGUILayout.PropertyField(m_SkillDisplayParam, includeChildren: true);
        EditorGUILayout.PropertyField(m_TargetMode);
        EditorGUILayout.PropertyField(_cooldown);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField(Styles.Cost, EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(costHP);
        EditorGUILayout.PropertyField(costMP);
        
        EditorGUILayout.EndVertical();
        
        // effect context
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandWidth(true));
        GUILayout.Label(Styles.defaultFontAssetLabel, EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(m_TreeRoot);
        // EditorGUILayout.PropertyField(m_Nodes);
        m_ReorderableList.DoLayoutList();
        
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();
        
        
        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties();
        }
    }
}

#endif

