#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public static class EditorTools
{
    [MenuItem("CustomTools/打开Main", false, 100)]
    public static void gotoLaunchScene()
    {
        EditorSceneManager.OpenScene("Assets/Launch.unity");
    }
}
#endif


