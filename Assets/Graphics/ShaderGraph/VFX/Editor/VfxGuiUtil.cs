#if UNITY_EDITOR

using System;
using UnityEditor;
using UnityEngine;

public enum CustomDataFloat {
    Unbind = 0,
    C1_X = 1,
    C1_Y = 2,
    C1_Z = 3,
    C1_W = 4,
    C2_X = 5,
    C2_Y = 6,
    C2_Z = 7,
    C2_W = 8,
}

public enum CustomDataVector2 {
    Unbind = 0,
    C1_XY = 1,
    C1_YZ = 2,
    C1_ZW = 3,
    C2_XY = 4,
    C2_YZ = 5,
    C2_ZW = 6,
}

public enum CustomDataVector3 {
    Unbind = 0,
    C1_XYZ = 1,
    C1_YZW = 2,
    C2_XYZ = 3,
    C2_YZW = 4,
}

public enum CustomDataVector4 {
    Unbind = 0,
    C1 = 1,
    C2 = 2,
}

public static class VfxGuiUtil {
    const float k_IndentMargin = 15.0f;
    
    public static readonly string[] CustomDataFloat = Enum.GetNames(typeof(CustomDataFloat));
    public static readonly string[] CustomDataVector2 = Enum.GetNames(typeof(CustomDataVector2));
    public static readonly string[] CustomDataVector3 = Enum.GetNames(typeof(CustomDataVector3));
    public static readonly string[] CustomDataVector4 = Enum.GetNames(typeof(CustomDataVector4));
    
    public static bool DrawHeaderFoldout(string title, bool state, float height, bool enabled = false) {
        var backgroundRect = GUILayoutUtility.GetRect(0f, height);

        var labelRect = backgroundRect;
        labelRect.xMin += 8f;
        labelRect.xMax -= 20f;

        var foldoutRect = backgroundRect;
        foldoutRect.y += 11f;
        foldoutRect.width = 13f;
        foldoutRect.height = 13f;
        foldoutRect.x = labelRect.xMin + k_IndentMargin * (EditorGUI.indentLevel - 1); //fix for presset


        var color = GUI.color;
        GUI.color = GetGroupColor(state, enabled);
        EditorGUI.HelpBox(backgroundRect, "", MessageType.None);
        GUI.color = color;
        // Title
        EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

        var e = Event.current;

        if (e.type == EventType.MouseDown) {
            if (backgroundRect.Contains(e.mousePosition)) {
                if (e.button == 0) {
                    state = !state;
                    e.Use();
                }
        
                e.Use();
            }
        }

        return state;
    }
    
    private static readonly Color GroupColorEnabled = new(0, 1, 0, 10);
    private static readonly Color GroupColorDisabled = new(1, 0, 0, 10);

    private static Color GetGroupColor(bool expand, bool enabled) {
        var baseColor = enabled ? GroupColorEnabled : GroupColorDisabled;
        return expand ? baseColor * 0.5f : baseColor;
    }
}

#endif