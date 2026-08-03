/*
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestTemplateAsset))]
public class QuestTemplateAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var asset = (QuestTemplateAsset)target;
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("模板工具", EditorStyles.boldLabel);

        if (GUILayout.Button("新建默认 Quest 到资源"))
        {
            Undo.RecordObject(asset, "New Default Quest");
            asset.quest = QuestEditorSampleData.CreateSampleQuest();
            EditorUtility.SetDirty(asset);
        }

        if (GUILayout.Button("仅清除接取/完成状态（模板化）"))
        {
            Undo.RecordObject(asset, "Strip Quest Runtime State");
            QuestTemplateRuntime.ApplyTemplateRuntimeDefaults(asset.quest);
            EditorUtility.SetDirty(asset);
        }
    }
}
#endif
*/
