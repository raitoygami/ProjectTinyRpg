/*
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// 自定义 <see cref="QuestPhase"/> 绘制：Goals 列表的 + 通过反射列出 <see cref="QuestGoal"/> 派生类型并 <c>Activator.CreateInstance</c>。
/// </summary>
[CustomPropertyDrawer(typeof(QuestPhase))]
public class QuestPhasePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float y = position.y;
        float line = EditorGUIUtility.singleLineHeight;
        float sp = EditorGUIUtility.standardVerticalSpacing;

        var phaseId = property.FindPropertyRelative("phaseId");
        float phaseH = EditorGUI.GetPropertyHeight(phaseId, true);
        EditorGUI.PropertyField(new Rect(position.x, y, position.width, phaseH), phaseId, new GUIContent("Phase Id"), true);
        y += phaseH + sp;

        var goals = property.FindPropertyRelative("goals");
        var list = GetList(property.serializedObject, goals);
        float listH = list.GetHeight();
        list.DoList(new Rect(position.x, y, position.width, listH));

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var phaseId = property.FindPropertyRelative("phaseId");
        var goals = property.FindPropertyRelative("goals");
        float h = EditorGUI.GetPropertyHeight(phaseId, true) + EditorGUIUtility.standardVerticalSpacing;
        h += GetList(property.serializedObject, goals).GetHeight();
        return h;
    }

    static readonly System.Collections.Generic.Dictionary<string, ReorderableList> ListByPath = new();

    static ReorderableList GetList(SerializedObject so, SerializedProperty goals)
    {
        string key = so.targetObject != null ? $"{so.targetObject.GetInstanceID()}:{goals.propertyPath}" : goals.propertyPath;

        if (!ListByPath.TryGetValue(key, out var list) || list.serializedProperty?.serializedObject != so)
        {
            ReorderableList rl = null;
            rl = new ReorderableList(so, goals, true, true, true, true)
            {
                drawHeaderCallback = rect => { EditorGUI.LabelField(rect, "Goals（+ 选择派生类型）"); },
                elementHeightCallback = index =>
                {
                    var sp = rl.serializedProperty;
                    if (index < 0 || index >= sp.arraySize) return line;
                    var elem = sp.GetArrayElementAtIndex(index);
                    return line + EditorGUI.GetPropertyHeight(elem, true);
                },
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var sp = rl.serializedProperty;
                    if (index < 0 || index >= sp.arraySize) return;
                    var elem = sp.GetArrayElementAtIndex(index);

                    var typeContent = GetGoalTypeLabelContent(elem);
                    var labelRect = new Rect(rect.x, rect.y, rect.width, line);
                    EditorGUI.LabelField(labelRect, typeContent, EditorStyles.boldLabel);

                    float bodyY = rect.y + line ;
                    float bodyH = rect.height - line ;
                    EditorGUI.PropertyField(new Rect(rect.x, bodyY, rect.width, bodyH), elem, GUIContent.none, true);
                },
                onAddDropdownCallback = (buttonRect, l) => ShowAddGoalMenu(l.serializedProperty),
                onRemoveCallback = l =>
                {
                    int i = l.index;
                    if (i < 0 || i >= l.serializedProperty.arraySize) return;
                    l.serializedProperty.DeleteArrayElementAtIndex(i);
                },
            };
            list = rl;
            ListByPath[key] = list;
        }
        else
        {
            list.serializedProperty = goals;
        }

        return list;
    }

    static void ShowAddGoalMenu(SerializedProperty goals)
    {
        var menu = new GenericMenu();
        foreach (var t in QuestGoalTypeCache.ConcreteQuestGoalTypes)
        {
            Type type = t;
            menu.AddItem(new GUIContent(type.Name), false, () =>
            {
                try
                {
                    object instance = Activator.CreateInstance(type);
                    if (instance is not QuestGoal)
                        return;
                    Undo.RecordObject(goals.serializedObject.targetObject, "Add Quest Goal");
                    int newIndex = goals.arraySize;
                    goals.InsertArrayElementAtIndex(newIndex);
                    var elem = goals.GetArrayElementAtIndex(newIndex);
                    elem.managedReferenceValue = instance;
                    goals.serializedObject.ApplyModifiedProperties();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[QuestPhase] 无法创建 {type.Name}：{e.Message}");
                }
            });
        }

        if (menu.GetItemCount() == 0)
            menu.AddDisabledItem(new GUIContent("无 QuestGoal 派生类型（检查程序集）"));

        menu.ShowAsContext();
    }

    /// <summary>SerializeReference 条目的短类型名，用于行首 Label；完整名放 tooltip。</summary>
    static GUIContent GetGoalTypeLabelContent(SerializedProperty elem)
    {
        if (elem == null)
            return new GUIContent("(null)", "");

        if (elem.propertyType != SerializedPropertyType.ManagedReference)
            return new GUIContent(elem.displayName, elem.propertyPath);

        if (elem.managedReferenceValue == null)
            return new GUIContent("（未指定类型）", "请使用列表 + 添加 QuestGoal 派生类型");

        string full = elem.managedReferenceFullTypename;
        if (string.IsNullOrEmpty(full))
            return new GUIContent("（未指定类型）", "");

        // 形如 "AssemblyName Namespace.TypeName"
        int space = full.IndexOf(' ');
        string asmQualified = space >= 0 ? full.Substring(space + 1) : full;
        int lastDot = asmQualified.LastIndexOf('.');
        string shortName = lastDot >= 0 ? asmQualified.Substring(lastDot + 1) : asmQualified;
        return new GUIContent($"类型: {shortName}", asmQualified);
    }

    static float line => EditorGUIUtility.singleLineHeight;
    static float sp => EditorGUIUtility.standardVerticalSpacing;
}
#endif
*/
