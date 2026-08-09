#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(Ability))]
public class AbilityInspector : Editor
{
    private SerializedProperty _abilityID;
    private SerializedProperty _name;
    private SerializedProperty _icon;
    private SerializedProperty _weaponTypeRequire;
    private SerializedProperty _prepareTurn;
    private SerializedProperty _castRange;
    private SerializedProperty _castTargetingMode;
    private SerializedProperty _castCenterType;
    private SerializedProperty _affectType;
    private SerializedProperty _range;
    private SerializedProperty _skillDisplayParam;
    private SerializedProperty _targetType;
    private SerializedProperty _cooldown;

    private SerializedProperty costMP;
    private SerializedProperty costHP;

    private SerializedProperty _treeRoot;
    private ReorderableList _reorderableList;

    private void OnEnable()
    {
        _abilityID = serializedObject.FindProperty("_abilityID");
        _name = serializedObject.FindProperty("AbilityName");
        _icon = serializedObject.FindProperty("Icon");
        _weaponTypeRequire = serializedObject.FindProperty("WeaponTypeRequire");
        _prepareTurn = serializedObject.FindProperty("_prepareTurn");
        _castRange = serializedObject.FindProperty("_castRange");
        _castTargetingMode = serializedObject.FindProperty("_castTargetingMode");
        _castCenterType = serializedObject.FindProperty("_castCenterType");
        _affectType = serializedObject.FindProperty("_affectType");
        _range = serializedObject.FindProperty("_range");
        _skillDisplayParam = serializedObject.FindProperty("m_SkillDisplayParam");
        _targetType = serializedObject.FindProperty("_targetType");
        _cooldown = serializedObject.FindProperty("_cooldown");

        costHP = serializedObject.FindProperty("CostHP");
        costMP = serializedObject.FindProperty("CostMP");

        _treeRoot = serializedObject.FindProperty("TreeRoot");
        var effects = serializedObject.FindProperty("Effects");

        // ReSharper disable once UseObjectOrCollectionInitializer
        _reorderableList = new ReorderableList(serializedObject, effects);
        _reorderableList.drawHeaderCallback = DrawHeader;
        _reorderableList.drawElementCallback = (rect, index, _, _) =>
        {
            EditorGUI.PropertyField(rect, effects.GetArrayElementAtIndex(index));
        };
        _reorderableList.displayAdd = false;
        _reorderableList.displayRemove = false;
    }

    private static void DrawHeader(Rect rect)
    {
        GUI.Label(rect, "");
    }

    internal static class Styles
    {
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
        EditorGUILayout.PropertyField(_abilityID);
        EditorGUILayout.PropertyField(_name);
        EditorGUILayout.PropertyField(_icon);
        EditorGUILayout.PropertyField(_weaponTypeRequire);
        EditorGUILayout.PropertyField(_prepareTurn);
        EditorGUILayout.PropertyField(_castRange);
        EditorGUILayout.PropertyField(_castTargetingMode);
        EditorGUILayout.PropertyField(_castCenterType);
        EditorGUILayout.PropertyField(_affectType);
        EditorGUILayout.PropertyField(_range);
        EditorGUILayout.PropertyField(_skillDisplayParam, true);
        EditorGUILayout.PropertyField(_targetType);
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
        EditorGUILayout.PropertyField(_treeRoot);
        // EditorGUILayout.PropertyField(m_Nodes);
        _reorderableList.DoLayoutList();

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndVertical();


        if (EditorGUI.EndChangeCheck()) serializedObject.ApplyModifiedProperties();
    }
}

#endif