using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class UIRoot : Singleton<UIRoot>
{
    private const string SettingsPanelKey = "Settings";

    [SerializeField] private Camera UICamera;
    private RectTransform _Root;

    public Camera GetUICamera()
    {
        return UICamera;
    }

    public MainUI m_MainUI;
    public ToolTipUI ToolTipUI;
    [SerializeField] private RectTransform _LayerCarry;

    [SerializeField] public UIDialogue Dialogue;

    [Header("可选：用于校验或引用")] [SerializeField]
    private Canvas _layoutCanvas;

    public Canvas LayoutCanvas => _layoutCanvas;

    [Header("面板父节点（Key = PanelAttribute.Root）")] [SerializeField]
    private UIRootPanelParentBinding[] _panelParents;

    /// <summary>已加载实例（PanelKey → 根物体），关闭时不移除。</summary>
    private readonly Dictionary<string, GameObject> _uiOpenedTable = new();

    private readonly Dictionary<string, Type> _panelTypeByKey = new();

    /// <summary>同键并发加载去重。</summary>
    private readonly Dictionary<string, UniTask> _loadingByKey = new();

    private readonly object _loadLock = new();

    /// <summary>仅含 <see cref="EscBehavior.CloseOnly" /> 的 ESC 顺序：栈顶为最近一次打开的一帧（单键或 <see cref="Toggle" /> 批量）。</summary>
    private readonly List<List<string>> _escCloseStack = new();

    public RectTransform GetLayerCarry()
    {
        return _LayerCarry;
    }

    private void Awake()
    {
        BuildPanelTypeRegistry();
        this.SubscribeInput<InputManager.EscPressedEvt>(OnEscPressed);
    }

    private async UniTask OnEscPressed(InputManager.EscPressedEvt _)
    {
        await HandleEscAsync();
    }

    private async UniTask HandleEscAsync()
    {
        if (_escCloseStack.Count > 0)
        {
            var frame = _escCloseStack[^1];
            _escCloseStack.RemoveAt(_escCloseStack.Count - 1);
            foreach (var k in frame)
                Hide(k, false);
            return;
        }

        if (IsPanelActive(SettingsPanelKey))
        {
            Hide(SettingsPanelKey, false);
            return;
        }

        await Open(SettingsPanelKey);
    }

    private void BuildPanelTypeRegistry()
    {
        _panelTypeByKey.Clear();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException)
            {
                continue;
            }

            foreach (var type in types)
            {
                if (type.IsAbstract || !typeof(PanelBase).IsAssignableFrom(type))
                    continue;
                var attr = type.GetCustomAttribute<PanelAttribute>();
                if (attr == null || string.IsNullOrEmpty(attr.PanelKey))
                    continue;
                if (_panelTypeByKey.ContainsKey(attr.PanelKey))
                {
                    Debug.LogWarning($"[UIRoot] Duplicate PanelKey \"{attr.PanelKey}\" on {type.Name}; ignoring.");
                    continue;
                }

                _panelTypeByKey[attr.PanelKey] = type;
            }
        }
    }

    private RectTransform ResolveParent(string rootKey)
    {
        if (string.IsNullOrEmpty(rootKey) || _panelParents == null)
            return null;
        foreach (var e in _panelParents)
        {
            if (e == null || string.IsNullOrEmpty(e.rootKey))
                continue;
            if (e.rootKey == rootKey && e.parent != null)
                return e.parent;
        }

        return null;
    }

    public RectTransform GetRoot()
    {
        if (_Root == null) _Root = GetComponent<RectTransform>();
        return _Root;
    }


    public async UniTask Open(string panelKey, bool skipEscStackRegistration = false)
    {
        if (string.IsNullOrEmpty(panelKey))
            return;

        if (!_panelTypeByKey.TryGetValue(panelKey, out var panelType))
        {
            Debug.LogWarning(
                $"[UIRoot] Unknown panel key \"{panelKey}\". Add a PanelBase subclass with [Panel] and matching PanelKey.");
            return;
        }

        var attr = panelType.GetCustomAttribute<PanelAttribute>();
        if (attr == null)
            return;

        var parent = ResolveParent(attr.Root);
        if (parent == null)
        {
            Debug.LogWarning(
                $"[UIRoot] No parent for Root=\"{attr.Root}\" (panel \"{panelKey}\"). Add a row in _panelParents.");
            return;
        }

        if (_uiOpenedTable.TryGetValue(panelKey, out var existing) && existing != null)
        {
            var wasActive = IsPanelActive(panelKey);
            ApplyMuteGroup(panelKey, panelType);
            ShowInstance(existing);
            if (!wasActive && !skipEscStackRegistration)
                TryRegisterEscClose(panelKey);
            return;
        }

        UniTask loadTask;
        lock (_loadLock)
        {
            if (_uiOpenedTable.TryGetValue(panelKey, out existing) && existing != null)
            {
                var wasActive = IsPanelActive(panelKey);
                ApplyMuteGroup(panelKey, panelType);
                ShowInstance(existing);
                if (!wasActive && !skipEscStackRegistration)
                    TryRegisterEscClose(panelKey);
                return;
            }

            if (!_loadingByKey.TryGetValue(panelKey, out loadTask))
            {
                loadTask = LoadAndRegisterAsync(panelKey, attr, parent);
                _loadingByKey[panelKey] = loadTask;
            }
        }

        try
        {
            await loadTask;
        }
        finally
        {
            lock (_loadLock)
            {
                _loadingByKey.Remove(panelKey);
            }
        }

        if (_uiOpenedTable.TryGetValue(panelKey, out existing) && existing != null)
        {
            ApplyMuteGroup(panelKey, panelType);
            ShowInstance(existing);
            if (!skipEscStackRegistration)
                TryRegisterEscClose(panelKey);
        }
    }

    private PanelAttribute GetPanelAttributeForKey(string panelKey)
    {
        if (!_panelTypeByKey.TryGetValue(panelKey, out var t))
            return null;
        return t.GetCustomAttribute<PanelAttribute>();
    }

    private void TryRegisterEscClose(string panelKey)
    {
        var attr = GetPanelAttributeForKey(panelKey);
        if (attr == null || attr.EscBehavior != EscBehavior.CloseOnly)
            return;
        _escCloseStack.Add(new List<string> { panelKey });
    }

    private void RemoveFromEscStack(string panelKey)
    {
        for (var i = _escCloseStack.Count - 1; i >= 0; i--)
        {
            var frame = _escCloseStack[i];
            if (!frame.Remove(panelKey))
                continue;
            if (frame.Count == 0)
                _escCloseStack.RemoveAt(i);
            return;
        }
    }

    /// <summary>已打开则关闭，否则打开（无需事先查询显隐）。</summary>
    public async UniTask Toggle(string panelKey)
    {
        if (string.IsNullOrEmpty(panelKey))
            return;
        if (IsPanelActive(panelKey))
            Hide(panelKey);
        else
            await Open(panelKey);
    }

    /// <summary>
    ///     若列表中界面均已打开则全部关闭；否则依次打开未就绪的界面，并作为一整帧压入 ESC 栈（一次 ESC 关闭本批全部）。
    /// </summary>
    public async UniTask Toggle(IReadOnlyList<string> panelKeys)
    {
        if (panelKeys == null || panelKeys.Count == 0)
            return;

        var keys = panelKeys.Where(k => !string.IsNullOrEmpty(k)).Distinct().ToList();
        if (keys.Count == 0)
            return;

        if (keys.All(IsPanelActive))
        {
            if (EscStackTopMatchesBatchInternal(keys))
                _escCloseStack.RemoveAt(_escCloseStack.Count - 1);
            else
                foreach (var k in keys)
                    RemoveFromEscStack(k);

            foreach (var k in keys)
                Hide(k, false);
            return;
        }

        foreach (var k in keys)
            await Open(k, true);
        PushEscBatchFrame(keys);
    }

    private bool EscStackTopMatchesBatchInternal(List<string> keys)
    {
        if (_escCloseStack.Count == 0)
            return false;
        var top = _escCloseStack[^1];
        return top.Count == keys.Count && new HashSet<string>(top).SetEquals(keys);
    }

    private void PushEscBatchFrame(List<string> keys)
    {
        if (keys == null || keys.Count == 0)
            return;
        _escCloseStack.Add(new List<string>(keys));
    }

    private async UniTask LoadAndRegisterAsync(string panelKey, PanelAttribute attr, RectTransform parent)
    {
        if (string.IsNullOrEmpty(attr.Address))
        {
            Debug.LogWarning($"[UIRoot] Panel \"{panelKey}\" has empty Address in PanelAttribute.");
            return;
        }

        var handle = Addressables.InstantiateAsync(attr.Address, parent);
        await UniTask.WaitUntil(() => handle.IsDone);
        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogWarning($"[UIRoot] Failed to load \"{attr.Address}\" for panel \"{panelKey}\".");
            return;
        }

        var go = handle.Result;
        _uiOpenedTable[panelKey] = go;
    }

    private void ShowInstance(GameObject go)
    {
        var pb = go.GetComponent<PanelBase>() ?? go.GetComponentInChildren<PanelBase>(true);
        if (pb != null)
            pb.Open();
        else
            go.SetActive(true);
    }

    private void ApplyMuteGroup(string openingKey, Type openingType)
    {
        var targetMute = openingType.GetCustomAttribute<PanelAttribute>()?.MuteGroup;
        if (string.IsNullOrEmpty(targetMute))
            return;

        var keys = new List<string>(_uiOpenedTable.Keys);
        foreach (var otherKey in keys)
        {
            if (otherKey == openingKey)
                continue;
            if (!_uiOpenedTable.TryGetValue(otherKey, out var go) || go == null)
                continue;
            if (!go.activeInHierarchy)
                continue;

            if (!_panelTypeByKey.TryGetValue(otherKey, out var otherType))
                continue;
            var otherMute = otherType.GetCustomAttribute<PanelAttribute>()?.MuteGroup;
            if (otherMute == targetMute)
                Hide(otherKey);
        }
    }

    public void Hide(string panelKey, bool updateEscStack = true)
    {
        if (string.IsNullOrEmpty(panelKey))
            return;
        if (!_uiOpenedTable.TryGetValue(panelKey, out var go) || go == null)
            return;

        if (updateEscStack)
            RemoveFromEscStack(panelKey);

        var lp = go.GetComponent<LootUI>() ?? go.GetComponentInChildren<LootUI>(true);
        if (lp != null && lp.IsOpen)
        {
            lp.Close();
            return;
        }

        var pb = go.GetComponent<PanelBase>() ?? go.GetComponentInChildren<PanelBase>(true);
        if (pb != null)
            pb.Close();
        else
            go.SetActive(false);
    }

    public bool IsPanelActive(string panelKey)
    {
        if (string.IsNullOrEmpty(panelKey))
            return false;
        if (!_uiOpenedTable.TryGetValue(panelKey, out var go) || go == null)
            return false;

        var lp = go.GetComponent<LootUI>() ?? go.GetComponentInChildren<LootUI>(true);
        if (lp != null)
            return lp.IsOpen;

        return go.activeInHierarchy;
    }

    public T GetPanel<T>(string panelKey) where T : Component
    {
        if (string.IsNullOrEmpty(panelKey))
            return null;
        if (!_uiOpenedTable.TryGetValue(panelKey, out var go) || go == null)
            return null;
        var c = go.GetComponent<T>();
        return c != null ? c : go.GetComponentInChildren<T>(true);
    }

    /// <summary>
    ///     关闭并销毁所有已加载的面板实例，清理所有缓存和ESC栈。
    /// </summary>
    public async UniTask CloseAllAsync()
    {
        // 1. 等待所有正在进行的加载任务完成
        List<UniTask> loadingTasks;
        lock (_loadLock)
        {
            loadingTasks = _loadingByKey.Values.ToList();
        }

        if (loadingTasks.Count > 0) await UniTask.WhenAll(loadingTasks);

        // 2. 收集所有有效实例并清空字典/栈
        List<GameObject> instancesToDestroy;
        lock (_loadLock)
        {
            instancesToDestroy = _uiOpenedTable.Values.Where(go => go != null).ToList();
            _uiOpenedTable.Clear();
            _loadingByKey.Clear();
            _escCloseStack.Clear();
        }

        // 3. 销毁所有实例（使用 Addressables 释放）
        foreach (var go in instancesToDestroy)
            if (go != null)
                Addressables.ReleaseInstance(go); // 自动 Destroy 并释放资源
    }


    public LootUI LootUI => GetPanel<LootUI>("Loot");

    public InventoryUI InventoryUI => GetPanel<InventoryUI>("Inventory");
    /*public async UniTask OpenLootPanel(LootUnit lootUnit)
    {
        if (lootUnit == null)
            return;
        await Open("Loot");
        var lp = LootUI;
        //lp?.Open(lootUnit);
    }*/

    public void CloseLootPanel()
    {
        var lp = LootUI;
        if (lp != null && lp.IsOpen)
            lp.Close();
        else
            Hide("Loot");
    }


    [SerializeField] private Image _FadingPanel;
    private static readonly int DissolveThreshold = Shader.PropertyToID("_DissolveThreshold");

    public async UniTask FadeIn(float duration = 1.5f, Ease ease = Ease.OutQuad)
    {
        _FadingPanel.color = new Color(0, 0, 0, 0);
        _FadingPanel.gameObject.SetActive(true);
        await DOTween.To(
                () => _FadingPanel.color, // getter
                c => _FadingPanel.color = c, // setter
                Color.black, // 目标颜色
                duration // 时长
            )
            .SetEase(ease)
            .ToUniTask();
    }

    public async UniTask FadeOut(float duration = 1.5f, Ease ease = Ease.OutQuad)
    {
        _FadingPanel.color = Color.black;

        await DOTween.To(
                () => _FadingPanel.color, // getter
                c => _FadingPanel.color = c, // setter
                new Color(0, 0, 0, 0), // 目标颜色
                duration // 时长
            )
            .SetEase(ease)
            .ToUniTask();
        _FadingPanel.color = Color.clear;
        _FadingPanel.gameObject.SetActive(false);
    }

    [SerializeField] private Image _LoadingPanel;

    public async UniTask LoadingStart(float duration = 1.5f, Ease ease = Ease.OutQuad)
    {
        _LoadingPanel.material.SetFloat(DissolveThreshold, 0);
        _LoadingPanel.gameObject.SetActive(true);
        // 使用 DOTween 动画 Material 的 _DissolveThreshold
        Tween tween = _LoadingPanel.material
            .DOFloat(1f, DissolveThreshold, duration) // 从当前值 → 1
            .SetEase(ease);

        await tween.ToUniTask(); // 关键：转成 UniTask
    }

    public async UniTask LoadingFinish(float duration = 1.5f, Ease ease = Ease.InQuad)
    {
        _LoadingPanel.material.SetFloat(DissolveThreshold, 1);

        // 使用 DOTween 动画 Material 的 _DissolveThreshold
        Tween tween = _LoadingPanel.material
            .DOFloat(0f, "_DissolveThreshold", duration)
            .SetEase(ease);

        await tween.ToUniTask();

        _LoadingPanel.gameObject.SetActive(false);
    }

    public MainUI GetMainUI() => m_MainUI;
    
    public void OpenMainUI()
    {
        m_MainUI.gameObject.SetActive(true);
        m_MainUI.OnRefresh();
    }

    public void CloseMainUI()
    {
        m_MainUI.gameObject.SetActive(false);
    }

    public async UniTask OpenStartMenu()
    {
        await Open("StartMenu");
    }

    protected override void OnRelease()
    {
        transform.DOKill();
    }
}