/*
using System.Collections.Generic;
using System.Linq;
using cfg;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

/// <summary>
///     基于 LootUnit 预制体与 item 配置在 Entity 周围创建掉落实体；数据存于 <see cref="GameData.dropScenes"/>。
///     同格可合并为单个 <see cref="LootUnit"/>（同一 <see cref="DropPile"/>）；运行时按场景+格子索引 GameObject。
///     合并到已有格时仍会生成仅用于抛物线演示的临时 <see cref="LootUnit"/>，结束后入池或销毁。
/// </summary>
public class DropSystem : Singleton<DropSystem>
{
    private const int DropFxPoolMaxSize = 16;

    private LootUnit _dropPrefab;

    /// <summary>重叠掉落动画用实例池（无碰撞、不入格点索引）。</summary>
    private readonly Queue<GameObject> _dropFxPool = new();

    private Transform _dropFxPoolRoot;

    /// <summary>场景 → (格 x,z) → 掉落实例（每格至多一个 LootUnit）。</summary>
    private readonly Dictionary<string, Dictionary<Vector2Int, GameObject>> _lootByGrid = new();

    /// <summary>场景内所有掉落实例根物体（用于批量销毁）。</summary>
    private readonly Dictionary<string, List<GameObject>> _lootRootsByScene = new();

    private GameData RuntimeData =>
        PersistenceModule.HasInstance() ? PersistenceModule.Instance.GetRuntimeData() : null;

    public void SetDropPrefab(LootUnit dropPrefab)
    {
        _dropPrefab = dropPrefab;
    }

    protected override void OnRelease()
    {
        ClearDropFxPool();
        base.OnRelease();
    }

    /// <summary>合并掉落到 LootUnit 后，若 LootPanel 正在显示同一个 LootUnit，则刷新其网格布局。</summary>
    static void NotifyLootPanelIfOpen(LootUnit lu)
    {
        if (lu == null || !UIRoot.HasInstance()) return;
        var lp = UIRoot.Instance.LootUI;
        /*if (lp != null && lp.IsOpen && lp.CurrentLootUnit == lu)
            lp.RefreshFromLootUnit();#1#
    }

    // ── Drop FX pool ────────────────────────────────────────────────────

    private void EnsureDropFxPoolRoot()
    {
        if (_dropFxPoolRoot != null) return;
        var root = new GameObject("[DropFxPool]");
        root.transform.SetParent(transform, false);
        root.SetActive(false);
        _dropFxPoolRoot = root.transform;
    }

    private GameObject RentDropFxVisual()
    {
        if (_dropPrefab == null) return null;
        EnsureDropFxPoolRoot();
        GameObject go;
        if (_dropFxPool.Count > 0)
            go = _dropFxPool.Dequeue();
        else
            go = Instantiate(_dropPrefab.gameObject);

        go.transform.SetParent(null, true);
        go.SetActive(true);

        foreach (var c in go.GetComponentsInChildren<Collider>(true))
            c.enabled = false;
        foreach (var ai in go.GetComponentsInChildren<AgentInteractive>(true))
            ai.enabled = false;

        return go;
    }

    private void ReturnDropFxVisual(GameObject go)
    {
        if (go == null || _dropPrefab == null) return;
        go.transform.DOKill();
        var lu = go.GetComponent<LootUnit>();
        lu?.ClearForPool();
        go.SetActive(false);
        EnsureDropFxPoolRoot();
        go.transform.SetParent(_dropFxPoolRoot, false);
        if (_dropFxPool.Count < DropFxPoolMaxSize)
            _dropFxPool.Enqueue(go);
        else
            Destroy(go);
    }

    private void ClearDropFxPool()
    {
        while (_dropFxPool.Count > 0)
        {
            var go = _dropFxPool.Dequeue();
            if (go != null)
                Destroy(go);
        }

        if (_dropFxPoolRoot != null)
        {
            Destroy(_dropFxPoolRoot.gameObject);
            _dropFxPoolRoot = null;
        }
    }

    // ── Grid registry ───────────────────────────────────────────────────

    private bool HasLootAtGrid(string sceneName, Vector2Int key)
    {
        if (!_lootByGrid.TryGetValue(sceneName, out var m) || m == null) return false;
        return m.TryGetValue(key, out var go) && go != null;
    }

    private bool TryGetLootGo(string sceneName, Vector2Int key, out GameObject go)
    {
        go = null;
        if (!_lootByGrid.TryGetValue(sceneName, out var m) || m == null) return false;
        return m.TryGetValue(key, out go) && go != null;
    }

    private void RegisterLootAtGrid(string sceneName, Vector2Int key, GameObject go)
    {
        if (!_lootByGrid.TryGetValue(sceneName, out var m))
        {
            m = new Dictionary<Vector2Int, GameObject>();
            _lootByGrid[sceneName] = m;
        }

        if (m.TryGetValue(key, out var oldGo) && oldGo != null && oldGo != go)
        {
            RemoveLootRootReference(sceneName, oldGo);
            Destroy(oldGo);
        }

        m[key] = go;
    }

    private void RemoveLootRootReference(string sceneName, GameObject target)
    {
        if (target == null) return;
        if (!_lootRootsByScene.TryGetValue(sceneName, out var roots)) return;
        roots.Remove(target);
    }

    private void AddLootRoot(string sceneName, GameObject go)
    {
        if (!_lootRootsByScene.TryGetValue(sceneName, out var list))
        {
            list = new List<GameObject>();
            _lootRootsByScene[sceneName] = list;
        }

        list.Add(go);
    }

    // ── Scene management ────────────────────────────────────────────────

    internal void ReleaseSceneDropGameObjects(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (_lootRootsByScene.TryGetValue(sceneName, out var list))
        {
            foreach (var go in list)
                if (go != null)
                    Destroy(go);
            _lootRootsByScene.Remove(sceneName);
        }

        _lootByGrid.Remove(sceneName);
    }

    public void DestroyAllRuntimeDropGameObjects()
    {
        foreach (var list in _lootRootsByScene.Values)
        foreach (var go in list)
            if (go != null)
                Destroy(go);
        _lootRootsByScene.Clear();
        _lootByGrid.Clear();
        ClearDropFxPool();
    }

    // ── Spawn from persistence ──────────────────────────────────────────

    public int SpawnAllDropsForScene(string sceneName)
    {
        if (_dropPrefab == null || string.IsNullOrEmpty(sceneName)) return 0;
        var data = RuntimeData;
        if (data == null || !data.TryGetDropSceneData(sceneName, out var sceneData)) return 0;

        var count = 0;
        foreach (var pile in sceneData.Piles)
        {
            if (pile == null || pile.IsEmpty) continue;
            var key = new Vector2Int(pile.gridX, pile.gridZ);
            if (HasLootAtGrid(sceneName, key)) continue;

            var worldPos = pile.GridPosition.GridToWorld();
            var go = Instantiate(_dropPrefab.gameObject, worldPos, Quaternion.identity);
            var lu = go.GetComponent<LootUnit>();
            if (lu != null)
                RestoreLootUnitFromPile(lu, pile);
            RegisterLootAtGrid(sceneName, key, go);
            AddLootRoot(sceneName, go);
            count++;
        }

        return count;
    }

    public void SpawnDropGameObjectsFromData(SceneDropData[] scenes)
    {
        if (scenes == null || _dropPrefab == null) return;
        foreach (var scene in scenes)
        {
            if (scene == null || string.IsNullOrEmpty(scene.sceneName)) continue;
            var sceneName = scene.sceneName;
            foreach (var pile in scene.Piles)
            {
                if (pile == null || pile.IsEmpty) continue;
                var key = new Vector2Int(pile.gridX, pile.gridZ);
                if (HasLootAtGrid(sceneName, key)) continue;

                var worldPos = pile.GridPosition.GridToWorld();
                var go = Instantiate(_dropPrefab.gameObject, worldPos, Quaternion.identity);
                var lu = go.GetComponent<LootUnit>();
                if (lu != null)
                    RestoreLootUnitFromPile(lu, pile);
                RegisterLootAtGrid(sceneName, key, go);
                AddLootRoot(sceneName, go);
            }
        }
    }

    static void RestoreLootUnitFromPile(LootUnit lu, DropPile pile)
    {
        lu.Stacks.Clear();
        foreach (var s in pile.items)
        {
            if (s == null || s.IsEmpty) continue;
            lu.AddOrMergeLoot(s.ItemId, s.Count);
        }
    }

    // ── Clear ───────────────────────────────────────────────────────────

    public bool ClearDropsInScene(string sceneName)
    {
        return RuntimeData != null && RuntimeData.RemoveDropSceneData(sceneName);
    }

    public void ClearAllDrops()
    {
        RuntimeData?.ClearAllDropSceneData();
    }

    // ── Query ───────────────────────────────────────────────────────────

    private string CurrentSceneName => SceneManager.GetActiveScene().name;

    public LootUnit GetLootUnit(Vector3 grid) => GetLootUnit(CurrentSceneName, (int)grid.x, (int)grid.z);

    public LootUnit GetLootUnit(int gridX, int gridZ) => GetLootUnit(CurrentSceneName, gridX, gridZ);

    public LootUnit GetLootUnit(string sceneName, int gridX, int gridZ)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        if (TryGetLootGo(sceneName, new Vector2Int(gridX, gridZ), out var go) && go != null)
            return go.GetComponent<LootUnit>();

        if (!_lootRootsByScene.TryGetValue(sceneName, out var roots) || roots == null || roots.Count == 0)
            return null;

        var inv = 1f / WorldExtensions.WorldToGridScale;
        foreach (var root in roots)
        {
            if (root == null) continue;
            var p = root.transform.position;
            var gx = Mathf.RoundToInt(p.x);
            var gz = Mathf.RoundToInt(p.z * inv);
            if (gx != gridX || gz != gridZ) continue;
            var lu = root.GetComponent<LootUnit>();
            if (lu != null) return lu;
        }

        return null;
    }

    // ── Remove / Destroy / Sync ─────────────────────────────────────────

    /// <summary>
    /// 从运行时网格映射、场景根列表及存档中移除该 LootUnit 的数据（不销毁 GameObject）。
    /// 在 Destroy 之前调用。
    /// </summary>
    public void RemoveLootPile(LootUnit lootUnit)
    {
        if (lootUnit == null) return;
        var go = lootUnit.gameObject;
        var sceneName = go.scene.name;
        if (string.IsNullOrEmpty(sceneName)) return;

        Vector2Int? keyFound = null;
        if (_lootByGrid.TryGetValue(sceneName, out var gridMap) && gridMap != null)
        {
            foreach (var kv in gridMap)
            {
                if (kv.Value != go) continue;
                keyFound = kv.Key;
                break;
            }

            if (keyFound.HasValue)
                gridMap.Remove(keyFound.Value);
        }

        RemoveLootRootReference(sceneName, go);

        if (keyFound.HasValue && RuntimeData != null &&
            RuntimeData.TryGetDropSceneData(sceneName, out var sceneData))
            sceneData.RemovePileAtGrid(keyFound.Value.x, keyFound.Value.y);
    }

    /// <summary>
    /// 用 LootUnit 当前 Stacks 重写该格的存档堆数据。
    /// </summary>
    public void SyncDropEntriesFromLootUnit(LootUnit lootUnit)
    {
        if (lootUnit == null) return;
        var go = lootUnit.gameObject;
        var sceneName = go.scene.name;
        if (string.IsNullOrEmpty(sceneName)) return;

        Vector2Int? keyFound = null;
        if (_lootByGrid.TryGetValue(sceneName, out var gridMap) && gridMap != null)
        {
            foreach (var kv in gridMap)
            {
                if (kv.Value != go) continue;
                keyFound = kv.Key;
                break;
            }
        }

        if (!keyFound.HasValue) return;

        var data = RuntimeData;
        if (data == null) return;
        if (!data.TryGetDropSceneData(sceneName, out var sceneData))
        {
            if (lootUnit.Stacks.Count == 0) return;
            sceneData = data.GetOrCreateDropSceneData(sceneName);
        }

        sceneData.SyncPileFromLootUnit(keyFound.Value.x, keyFound.Value.y, lootUnit.Stacks);
    }

    /// <summary>
    /// 完全移除并销毁该 LootUnit。
    /// </summary>
    public void DestroyLootPile(LootUnit lootUnit)
    {
        if (lootUnit == null) return;
        RemoveLootPile(lootUnit);
        Destroy(lootUnit.gameObject);
    }

    private SceneDropData GetOrCreateSceneData()
    {
        return RuntimeData?.GetOrCreateDropSceneData(CurrentSceneName);
    }

    // ── Loot rolling ────────────────────────────────────────────────────

    /// <summary>
    /// 根据 <see cref="AgentStats.EntityConfig"/> 的 <c>drop_id</c> 查 <see cref="Data_Drop"/>，
    /// 对每条 <see cref="Drop"/> 按 <c>chance</c>（百分比）判定，成功则数量在 <c>min~max</c> 间随机，再生成掉落实体。
    /// </summary>
    public int Drop(Entity dropper, int radius = 2)
    {
        if (dropper == null || _dropPrefab == null) return 0;
        var tables = ConfigManager.HasInstance() ? ConfigManager.Instance.Tables : null;
        var loots = RollLootItems(stats: dropper.GetComponent<AgentStats>(), tables);
        if (loots.Count == 0) return 0;
        return DropWithLootItems(dropper, loots, radius);
    }

    public static List<(int itemId, int count)> RollLootItems(AgentStats stats, Tables tables)
    {
        var list = new List<(int itemId, int count)>();
        if (stats?.EntityConfig?.DropId == null || tables == null) return list;

        var dropGroup = tables.DataDrop.GetOrDefault(stats.EntityConfig.DropId.Value);
        if (dropGroup?.Drops == null) return list;

        foreach (var d in dropGroup.Drops)
        {
            if (d == null || d.ItemId <= 0) continue;
            if (!RollChancePercent(d.Chance)) continue;
            var count = d.Max >= d.Min ? Random.Range(d.Min, d.Max + 1) : d.Min;
            if (count <= 0) continue;
            list.Add((d.ItemId, count));
        }

        return list;
    }

    private static bool RollChancePercent(int chancePercent)
    {
        if (chancePercent <= 0) return false;
        if (chancePercent >= 100) return true;
        return Random.Range(0, 100) < chancePercent;
    }

    private static Vector2Int ToGridKey(Vector3 gridVec) =>
        new(Mathf.RoundToInt(gridVec.x), Mathf.RoundToInt(gridVec.z));

    private static List<Vector3> DeduplicateGridCandidatesPreservingOrder(List<Vector3> raw)
    {
        var seen = new HashSet<Vector2Int>();
        var list = new List<Vector3>(raw.Count);
        foreach (var g in raw)
        {
            var k = ToGridKey(g);
            if (!seen.Add(k)) continue;
            list.Add(new Vector3(k.x, 0, k.y));
        }

        return list;
    }

    private static Vector3 SelectGridForLoot(
        List<Vector3> candidates,
        int placementIndex)
    {
        return candidates[placementIndex % candidates.Count];
    }

    // ── Runtime drop ────────────────────────────────────────────────────

    public int DropItemStackFromEntity(Entity dropper, int itemId, int count, int radius = 2)
    {
        if (dropper == null || itemId <= 0 || count <= 0)
            return 0;
        var loots = new List<(int itemId, int count)> { (itemId, count) };
        return DropWithLootItems(dropper, loots, radius);
    }

    private int DropWithLootItems(
        Entity dropper,
        List<(int itemId, int count)> loots,
        int radius = 2)
    {
        if (dropper == null || _dropPrefab == null || loots == null || loots.Count == 0)
            return 0;

        var effectiveNeed = loots.Count(t => t.itemId > 0);
        if (effectiveNeed == 0)
            return 0;
        var rawCandidates = GetEmptyGridsAround(dropper, effectiveNeed, radius);
        if (rawCandidates.Count == 0)
            return 0;

        var sceneData = GetOrCreateSceneData();
        if (sceneData == null) return 0;
        var dropperWorld = dropper.transform.position;
        var total = 0;
        var placementIndex = 0;

        foreach (var (itemId, count) in loots)
        {
            if (itemId <= 0) continue;
            var countSafe = count > 0 ? count : 1;

            var gridPos = SelectGridForLoot(rawCandidates, placementIndex);
            placementIndex++;
            var key = ToGridKey(gridPos);

            var gx = key.x;
            var gz = key.y;
            var targetWorld = gridPos.GridToWorld();

            if (TryGetLootGo(CurrentSceneName, key, out var existingGo) && existingGo != null)
            {
                var mergeLu = existingGo.GetComponent<LootUnit>();
                if (mergeLu != null)
                {
                    total++;
                    var fxGo = RentDropFxVisual();
                    if (fxGo == null)
                    {
                        mergeLu.AddOrMergeLoot(itemId, countSafe);
                        mergeLu.PlayMergePulse();
                        SyncPileFromLootUnit(sceneData, gx, gz, mergeLu);
                        NotifyLootPanelIfOpen(mergeLu);
                        continue;
                    }

                    var fxLu = fxGo.GetComponent<LootUnit>();
                    if (fxLu != null)
                    {
                        fxLu.SetLootItem(itemId, countSafe);
                        var pileGoRef = existingGo;
                        var pileLuRef = mergeLu;
                        fxLu.Drop(dropperWorld, targetWorld, () =>
                        {
                            if (pileLuRef != null && pileGoRef != null)
                            {
                                pileLuRef.AddOrMergeLoot(itemId, countSafe);
                                pileLuRef.PlayMergePulse();
                                SyncPileFromLootUnit(sceneData, gx, gz, pileLuRef);
                                NotifyLootPanelIfOpen(pileLuRef);
                            }

                            ReturnDropFxVisual(fxGo);
                        });
                    }
                    else
                    {
                        ReturnDropFxVisual(fxGo);
                        mergeLu.AddOrMergeLoot(itemId, countSafe);
                        mergeLu.PlayMergePulse();
                        SyncPileFromLootUnit(sceneData, gx, gz, mergeLu);
                        NotifyLootPanelIfOpen(mergeLu);
                    }

                    continue;
                }
            }

            var go = Instantiate(_dropPrefab.gameObject, dropperWorld, Quaternion.identity);
            var lootUnit = go.GetComponent<LootUnit>();
            if (lootUnit != null)
            {
                lootUnit.SetLootItem(itemId, countSafe);
                lootUnit.Drop(dropperWorld, targetWorld);
            }

            total++;
            RegisterLootAtGrid(CurrentSceneName, key, go);
            AddLootRoot(CurrentSceneName, go);

            if (lootUnit != null)
                SyncPileFromLootUnit(sceneData, gx, gz, lootUnit);
        }

        return total;
    }

    static void SyncPileFromLootUnit(SceneDropData sceneData, int gx, int gz, LootUnit lu)
    {
        sceneData?.SyncPileFromLootUnit(gx, gz, lu.Stacks);
    }

    // ── Grid helpers ────────────────────────────────────────────────────

    public List<Vector3> GetEmptyGridsAround(Entity entity, int needCount, int radius = 2)
    {
        var list = new List<Vector3>();
        if (entity == null || !Navigation.HasInstance())
            return list;

        var center = entity.GridPosition;
        var cx = Mathf.RoundToInt(center.x);
        var cz = Mathf.RoundToInt(center.z);
        var layer = Const.Layer.ObstacleForNavi;

        for (var dx = -radius; dx <= radius; dx++)
        for (var dz = -radius; dz <= radius; dz++)
        {
            var x = cx + dx;
            var z = cz + dz;
            var node = Navigation.Instance.GetNode(x, z);
            if (node == null) continue;
            var canUse = dx == 0 && dz == 0 || Navigation.IsWalkable(node, layer);
            if (!canUse) continue;
            list.Add(new Vector3(x, 0, z));
        }

        if (list.Count == 0) return list;

        int Dist(Vector3 g)
        {
            return Mathf.Abs((int)g.x - cx) + Mathf.Abs((int)g.z - cz);
        }

        list.Sort((a, b) => Dist(a).CompareTo(Dist(b)));

        var result = new List<Vector3>(needCount);
        for (var i = 0; i < needCount && i < list.Count; i++)
            result.Add(list[i]);
        while (result.Count < needCount)
            result.Add(list[Random.Range(0, list.Count)]);
        return result;
    }
}
*/
