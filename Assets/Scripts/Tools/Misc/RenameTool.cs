
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class RenameTool
{
    [MenuItem("Raitoygami/Tools/RenameAll")]
    public static void RenameAll(){
        var selections = Selection.objects;
        var baseName = selections[0].name;
        for (var i = 0; i < selections.Length; i++){
            Selection.objects[i].name = $"{baseName}{i:D2}";
        }
    }
}

#endif