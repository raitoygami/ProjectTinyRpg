using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局 GameObject 对象池管理器。按 prefab 实例 ID 分池缓存；定期分帧清理长时间未使用的缓存对象。
/// <para>
/// 使用方式：
/// <code>
/// var go = CacheManager.Instance.Get(prefab);          // 取出或实例化
/// CacheManager.Instance.Release(go, prefab);           // 归还到池
/// var comp = CacheManager.Instance.Get&lt;T&gt;(prefab);     // 取出并获取组件
/// CacheManager.Instance.Release(comp.gameObject, prefab); // 归还
/// </code>
/// 归还后对象被 deactivate 并挂到 CacheManager 自身节点下。
/// </para>
/// </summary>
public class CacheManager : Singleton<CacheManager>
{
    [Tooltip("缓存清理扫描间隔（秒）")]
    [SerializeField] float _cleanupInterval = 300f;

    [Tooltip("缓存对象闲置多久后被视为过期（秒）")]
    [SerializeField] float _expireTime = 20f;

    [Tooltip("单帧最多销毁多少个缓存对象（分帧处理避免卡顿）")]
    [SerializeField] int _maxDestroyPerFrame = 5;

    float _nextCleanupTime;

    readonly Dictionary<int, Pool> _pools = new();

    struct CacheEntry
    {
        public GameObject Go;
        public float ReleaseTime;
    }

    class Pool
    {
        public readonly List<CacheEntry> Idle = new();
    }

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>从池中取出一个 GameObject（或 Instantiate）。返回的对象 SetActive(true)。</summary>
    public GameObject Get(GameObject prefab)
    {
        if (prefab == null)
            return null;
        var key = prefab.GetInstanceID();
        if (_pools.TryGetValue(key, out var pool))
        {
            while (pool.Idle.Count > 0)
            {
                var last = pool.Idle.Count - 1;
                var entry = pool.Idle[last];
                pool.Idle.RemoveAt(last);
                if (entry.Go != null)
                {
                    entry.Go.SetActive(true);
                    return entry.Go;
                }
            }
        }

        return Instantiate(prefab);
    }

    /// <summary>从池中取出一个 GameObject 并返回指定组件。</summary>
    public T Get<T>(T prefab) where T : Component
    {
        if (prefab == null)
            return null;
        var go = Get(prefab.gameObject);
        return go != null ? go.GetComponent<T>() : null;
    }

    /// <summary>将不再使用的 GameObject 归还到池中。</summary>
    public void Release(GameObject go, GameObject prefab)
    {
        if (go == null)
            return;
        go.SetActive(false);
        go.transform.SetParent(transform, false);

        var key = prefab != null ? prefab.GetInstanceID() : 0;
        if (!_pools.TryGetValue(key, out var pool))
        {
            pool = new Pool();
            _pools[key] = pool;
        }

        pool.Idle.Add(new CacheEntry { Go = go, ReleaseTime = Time.unscaledTime });
    }

    /// <summary>将不再使用的组件所在 GameObject 归还到池中。</summary>
    public void Release<T>(T comp, T prefab) where T : Component
    {
        if (comp == null)
            return;
        Release(comp.gameObject, prefab != null ? prefab.gameObject : null);
    }

    /// <summary>清空指定 prefab 的所有缓存（立即销毁）。</summary>
    public void ClearPool(GameObject prefab)
    {
        if (prefab == null)
            return;
        var key = prefab.GetInstanceID();
        if (!_pools.TryGetValue(key, out var pool))
            return;
        foreach (var e in pool.Idle)
        {
            if (e.Go != null)
                Destroy(e.Go);
        }
        pool.Idle.Clear();
        _pools.Remove(key);
    }

    /// <summary>清空所有池（立即销毁全部缓存对象）。</summary>
    public void ClearAll()
    {
        foreach (var kv in _pools)
        {
            foreach (var e in kv.Value.Idle)
            {
                if (e.Go != null)
                    Destroy(e.Go);
            }
        }
        _pools.Clear();
    }

    // ── Unity lifecycle ─────────────────────────────────────────────────

    void Update()
    {
        if (Time.unscaledTime < _nextCleanupTime)
            return;
        _nextCleanupTime = Time.unscaledTime + _cleanupInterval;
        CleanupExpiredCoroutine();
    }

    void CleanupExpiredCoroutine()
    {
        var now = Time.unscaledTime;
        var destroyedThisFrame = 0;
        var keys = new List<int>(_pools.Keys);

        foreach (var key in keys)
        {
            if (!_pools.TryGetValue(key, out var pool))
                continue;

            for (var i = pool.Idle.Count - 1; i >= 0; i--)
            {
                var entry = pool.Idle[i];
                if (now - entry.ReleaseTime < _expireTime)
                    continue;

                pool.Idle.RemoveAt(i);
                if (entry.Go != null)
                {
                    Destroy(entry.Go);
                    destroyedThisFrame++;
                    if (destroyedThisFrame >= _maxDestroyPerFrame)
                    {
                        return;
                    }
                }
            }

            if (pool.Idle.Count == 0)
                _pools.Remove(key);
        }
    }

    protected override void OnRelease()
    {
        StopAllCoroutines();
        ClearAll();
    }
}
