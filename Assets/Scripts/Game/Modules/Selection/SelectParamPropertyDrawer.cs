#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="SerializeReference"/> 的 <see cref="SelectParam"/>：空引用时选类型；非空时 <c>Kind</c> 变更会切换为对应派生类型实例。
/// </summary>
[CustomPropertyDrawer(typeof(SelectParam))]
public class SelectParamPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
            return;
        }

        if (property.managedReferenceValue == null)
        {
            DrawNullSelector(position, property, label);
        }
        else
        {
            DrawManagedReferenceBody(position, property, label);
        }

        EditorGUI.EndProperty();
    }

    static void DrawManagedReferenceBody(Rect position, SerializedProperty property, GUIContent label)
    {
        float y = position.y;
        float width = position.width;
        float line = EditorGUIUtility.singleLineHeight;
        float sp = EditorGUIUtility.standardVerticalSpacing;

        EditorGUI.LabelField(new Rect(position.x, y, width, line), label, EditorStyles.boldLabel);
        y += line + sp;

        EditorGUI.indentLevel++;
        try
        {
            var kindProp = property.FindPropertyRelative("_kind");
            if (kindProp != null)
            {
                float kindH = EditorGUI.GetPropertyHeight(kindProp, true);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(new Rect(position.x, y, width, kindH), kindProp, true);
                if (EditorGUI.EndChangeCheck())
                {
                    var newKind = (SelectParamKind)kindProp.enumValueIndex;
                    SyncKindToManagedType(property, newKind);
                    property.serializedObject.ApplyModifiedProperties();
                    return;
                }

                y += kindH + sp;
            }

            var end = property.GetEndProperty();
            var it = property.Copy();
            if (!it.NextVisible(true))
                return;

            do
            {
                if (it.name == "_kind")
                    continue;

                float ph = EditorGUI.GetPropertyHeight(it, true);
                EditorGUI.PropertyField(new Rect(position.x, y, width, ph), it, true);
                y += ph + sp;
            } while (it.NextVisible(false) && !SerializedProperty.EqualContents(it, end));
        }
        finally
        {
            EditorGUI.indentLevel--;
        }
    }

    /// <summary>将 <see cref="SelectParamKind"/> 与 SerializeReference 实际类型对齐（必要时替换实例）。</summary>
    static void SyncKindToManagedType(SerializedProperty property, SelectParamKind kind)
    {
        var desiredType = SelectParamTypeCache.TypeForKind(kind);
        if (desiredType == null)
            return;

        var current = property.managedReferenceValue;
        if (current != null && current.GetType() == desiredType)
            return;

        try
        {
            var instance = Activator.CreateInstance(desiredType);
            if (instance is not SelectParam)
                return;

            Undo.RecordObject(property.serializedObject.targetObject, $"SelectParam Kind → {kind}");
            property.managedReferenceValue = instance;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SelectParam] 无法按 Kind={kind} 创建实例：{e.Message}");
        }
    }

    static void DrawNullSelector(Rect position, SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float sp = EditorGUIUtility.standardVerticalSpacing;

        var row = new Rect(position.x, position.y, position.width, line);
        var labelRect = new Rect(row.x, row.y, EditorGUIUtility.labelWidth, line);
        var rest = new Rect(row.x + EditorGUIUtility.labelWidth, row.y, row.width - EditorGUIUtility.labelWidth, line);

        EditorGUI.LabelField(labelRect, label);

        var btnRect = new Rect(rest.x, rest.y, Mathf.Min(rest.width, 200f), line);
        if (GUI.Button(btnRect, "选择 SelectParam 类型…", EditorStyles.miniButton))
            ShowAssignMenu(property);

        if (SelectParamTypeCache.ConcreteTypes.Count == 0)
        {
            EditorGUI.HelpBox(
                new Rect(position.x, position.y + line + sp, position.width, line * 2 + sp),
                "未找到 SelectParam 的非抽象派生类型。请确保 SelectCircleParam / SelectSectorParam / SelectRectParam / SelectPointParam 已编译。",
                MessageType.Warning);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
            return EditorGUI.GetPropertyHeight(property, label, true);

        if (property.managedReferenceValue == null)
            return GetNullSelectorHeight();

        return GetManagedBodyHeight(property, label);
    }

    static float GetNullSelectorHeight()
    {
        float line = EditorGUIUtility.singleLineHeight;
        float sp = EditorGUIUtility.standardVerticalSpacing;
        float h = line;
        if (SelectParamTypeCache.ConcreteTypes.Count == 0)
            h += sp + line * 2 + sp;
        return h;
    }

    static float GetManagedBodyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float sp = EditorGUIUtility.standardVerticalSpacing;
        float h = line + sp;

        var kindProp = property.FindPropertyRelative("_kind");
        if (kindProp != null)
            h += EditorGUI.GetPropertyHeight(kindProp, true) + sp;

        var end = property.GetEndProperty();
        var it = property.Copy();
        if (it.NextVisible(true))
        {
            do
            {
                if (it.name == "_kind")
                    continue;
                h += EditorGUI.GetPropertyHeight(it, true) + sp;
            } while (it.NextVisible(false) && !SerializedProperty.EqualContents(it, end));
        }

        return Mathf.Max(line, h - sp);
    }

    static void ShowAssignMenu(SerializedProperty property)
    {
        var menu = new GenericMenu();
        foreach (var t in SelectParamTypeCache.ConcreteTypes)
        {
            Type type = t;
            menu.AddItem(new GUIContent(type.Name), false, () => AssignNew(property, type));
        }

        if (menu.GetItemCount() == 0)
            menu.AddDisabledItem(new GUIContent("无可用类型"));

        menu.ShowAsContext();
    }

    static void AssignNew(SerializedProperty property, Type type)
    {
        try
        {
            var instance = Activator.CreateInstance(type);
            if (instance is not SelectParam)
                return;

            Undo.RecordObject(property.serializedObject.targetObject, $"Set SelectParam ({type.Name})");
            property.managedReferenceValue = instance;
            property.serializedObject.ApplyModifiedProperties();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SelectParam] 无法创建 {type.Name}：{e.Message}");
        }
    }
}
#endif
