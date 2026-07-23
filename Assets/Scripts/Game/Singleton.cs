using UnityEngine;

public class Singleton<T> : PubSubActor where T : Singleton<T>
{
    private const string ROOT_NAME = "Framework";
    protected static T _instance;

    private static GameObject root
    {
        get
        {
            var singleRoot = GameObject.Find(ROOT_NAME);
            if (singleRoot != null) return singleRoot;
            singleRoot = new GameObject(ROOT_NAME);
            DontDestroyOnLoad(singleRoot);
            return singleRoot;
        }
    }

    public static T Instance
    {
        get
        {
            if (_instance != null || !Application.isPlaying) return _instance;
            var obj = FindFirstObjectByType<T>();
            if (obj != null)
            {
                obj.name = $"----------{obj.name}----------";
                _instance = obj.GetComponent<T>();
                var r = root;
                if (r != null)
                    _instance.transform.SetParent(r.transform);
                return _instance;
            }

            var parent = root;
            if (parent == null)
                return null;

            var gameObject = new GameObject($"----------{typeof(T).Name}----------");
            gameObject.transform.SetParent(parent.transform);
            _instance = gameObject.AddComponent<T>();

            return _instance;
        }
    }

    public static bool HasInstance()
    {
        return _instance != null;
    }

    public virtual void Initialized()
    {
    }

    protected virtual void OnRelease()
    {
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        _instance.OnRelease();
        _instance = null;
    }
}

#if UNITY_EDITOR
/// <summary>
/// DontDestroyOnLoad 的 Framework 根在退出 Play 时若仍残留，会触发 Unity 的 scene cleanup 警告；在 ExitingPlayMode 时显式销毁。
/// </summary>
[UnityEditor.InitializeOnLoad]
internal static class SingletonFrameworkEditorCleanup
{
    static SingletonFrameworkEditorCleanup()
    {
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
        var g = GameObject.Find("Framework");
        if (g != null)
            Object.DestroyImmediate(g);
    }
}
#endif
