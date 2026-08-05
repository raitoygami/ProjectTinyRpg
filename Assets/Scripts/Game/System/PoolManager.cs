using System.Collections.Generic;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();

    private Transform Pool;

    public override void Initialized()
    {
        Pool = new GameObject("Pool").transform;
        Pool.SetParent(transform);
        Pool.gameObject.SetActive(false);
    }

    /// <summary>
    ///     从池中取出对象（若池不存在或无可用的，则新建实例）
    /// </summary>
    public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        // 确保池存在
        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        // 尝试从队列中取一个未激活的对象（取出即出队）
        while (queue.Count > 0)
        {
            var obj = queue.Dequeue();
            if (obj == null) continue; // 被外部销毁，跳过

            if (!obj.activeSelf)
            {
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.transform.SetParent(parent);
                obj.transform.localScale = Vector3.one;
                obj.SetActive(true);
                return obj;
            }

            // 若对象意外激活（理论不会发生），放回队尾并继续查找
            queue.Enqueue(obj);
        }

        // 无可用对象 → 新建实例（直接返回，不入队）
        var newObj = Instantiate(prefab, position, rotation, parent);
        return newObj;
    }

    /// <summary>
    ///     回收对象（必须传入对应的 prefab，否则将直接销毁对象并警告）
    /// </summary>
    public void Return(GameObject obj, GameObject prefab)
    {
        if (obj == null || prefab == null) return;

        if (pools.TryGetValue(prefab, out var queue))
        {
            obj.SetActive(false);
            obj.transform.SetParent(Pool);
            obj.transform.localPosition = Vector3.zero;

            queue.Enqueue(obj);
        }
        else
        {
            // 你没有预热这个 prefab，不应该让它进池，直接销毁
            Debug.LogWarning($"PoolManager: 预制体 [{prefab.name}] 未预热，对象将被销毁。");
            Destroy(obj);
        }
    }
}