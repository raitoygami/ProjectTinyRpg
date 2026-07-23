/*
using System;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// GameData 上的掉落存档（顶层 <see cref="GameData.dropScenes"/>，与 <see cref="GameData.quests"/> 同一模式；由 <see cref="PersistenceModule"/> 序列化）。
/// </summary>
public partial class GameData
{
    public SceneDropData[] dropScenes;

    public void EnsureDropScenesInitialized()
    {
        if (dropScenes == null)
            dropScenes = Array.Empty<SceneDropData>();
    }

    /// <summary>获取或创建指定场景的掉落数据。</summary>
    public SceneDropData GetOrCreateDropSceneData(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return null;
        EnsureDropScenesInitialized();
        if (dropScenes != null)
        {
            for (int i = 0; i < dropScenes.Length; i++)
                if (dropScenes[i] != null && dropScenes[i].sceneName == sceneName)
                    return dropScenes[i];
        }

        var newScene = new SceneDropData { sceneName = sceneName };
        dropScenes = dropScenes == null || dropScenes.Length == 0
            ? new[] { newScene }
            : AppendScenes(dropScenes, newScene);
        return newScene;
    }

    public bool TryGetDropSceneData(string sceneName, out SceneDropData data)
    {
        data = null;
        if (string.IsNullOrEmpty(sceneName)) return false;
        EnsureDropScenesInitialized();
        if (dropScenes == null) return false;
        for (int i = 0; i < dropScenes.Length; i++)
            if (dropScenes[i] != null && dropScenes[i].sceneName == sceneName)
            {
                data = dropScenes[i];
                return true;
            }

        return false;
    }

    public bool RemoveDropSceneData(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;
        EnsureDropScenesInitialized();
        if (dropScenes == null) return false;
        for (int i = 0; i < dropScenes.Length; i++)
        {
            if (dropScenes[i] == null || dropScenes[i].sceneName != sceneName) continue;
            dropScenes[i].ClearPiles();
            dropScenes = RemoveSceneAt(dropScenes, i);
            return true;
        }

        return false;
    }

    public void ClearAllDropSceneData()
    {
        EnsureDropScenesInitialized();
        if (dropScenes == null) return;
        foreach (var s in dropScenes)
            s?.ClearPiles();
        dropScenes = Array.Empty<SceneDropData>();
    }

    private static SceneDropData[] RemoveSceneAt(SceneDropData[] arr, int index)
    {
        if (arr == null || index < 0 || index >= arr.Length) return arr ?? Array.Empty<SceneDropData>();
        if (arr.Length == 1) return Array.Empty<SceneDropData>();
        var next = new SceneDropData[arr.Length - 1];
        for (int i = 0, j = 0; i < arr.Length; i++)
            if (i != index)
                next[j++] = arr[i];
        return next;
    }

    private static SceneDropData[] AppendScenes(SceneDropData[] arr, SceneDropData item)
    {
        var next = new SceneDropData[arr.Length + 1];
        Array.Copy(arr, next, arr.Length);
        next[arr.Length] = item;
        return next;
    }

    public void DestroyAllDropGameObjects()
    {
        /*if (DropSystem.HasInstance())
            DropSystem.Instance.DestroyAllRuntimeDropGameObjects();#1#
    }

    private static readonly int _registerDropSerialization = RegisterDropSerialization();

    private static int RegisterDropSerialization()
    {
        RegisterSerializationCallbacks(DropOnBeforeSerialize, DropOnAfterDeserialize);
        return 0;
    }

    private static void DropOnBeforeSerialize(GameData d)
    {
        d.EnsureDropScenesInitialized();
        if (d.dropScenes == null) return;
        foreach (var s in d.dropScenes)
            s?.OnBeforeSerialize();
    }

    private static void DropOnAfterDeserialize(GameData d)
    {
        d.EnsureDropScenesInitialized();
        if (d.dropScenes == null) return;
        foreach (var s in d.dropScenes)
            s?.OnAfterDeserialize();
    }
}

/// <summary>同一格子上的掉落堆：格坐标 + 道具列表。</summary>
[Serializable]
public class DropPile
{
    public int gridX;
    public int gridZ;
    public List<ItemStack> items = new();

    [JsonIgnore]
    public Vector3 GridPosition => new Vector3(gridX, 0, gridZ);

    [JsonIgnore]
    public bool IsEmpty => items == null || items.Count == 0;
}

/// <summary>单场景掉落数据。</summary>
[Serializable]
public class SceneDropData : ISerializationCallbackReceiver
{
    public string sceneName;

    [SerializeField]
    [JsonProperty("piles")]
    private DropPile[] _pilesSerialized;

    private List<DropPile> _piles = new();

    [JsonIgnore]
    public IReadOnlyList<DropPile> Piles => _piles;

    public DropPile GetOrCreatePile(int gridX, int gridZ)
    {
        foreach (var p in _piles)
            if (p != null && p.gridX == gridX && p.gridZ == gridZ)
                return p;
        var pile = new DropPile { gridX = gridX, gridZ = gridZ };
        _piles.Add(pile);
        return pile;
    }

    public DropPile FindPile(int gridX, int gridZ)
    {
        foreach (var p in _piles)
            if (p != null && p.gridX == gridX && p.gridZ == gridZ)
                return p;
        return null;
    }

    public void RemovePileAtGrid(int gridX, int gridZ)
    {
        for (var i = _piles.Count - 1; i >= 0; i--)
        {
            var p = _piles[i];
            if (p != null && p.gridX == gridX && p.gridZ == gridZ)
                _piles.RemoveAt(i);
        }
    }

    /// <summary>用 LootUnit 当前 stacks 重写该格的堆数据；stacks 为空则移除该 pile。</summary>
    public void SyncPileFromLootUnit(int gridX, int gridZ, IReadOnlyList<ItemStack> stacks)
    {
        RemovePileAtGrid(gridX, gridZ);
        if (stacks == null || stacks.Count == 0) return;
        var pile = new DropPile { gridX = gridX, gridZ = gridZ };
        foreach (var s in stacks)
        {
            if (s == null || s.IsEmpty) continue;
            pile.items.Add(s.Clone());
        }
        if (pile.items.Count > 0)
            _piles.Add(pile);
    }

    public void ClearPiles()
    {
        _piles.Clear();
    }

    public void OnBeforeSerialize()
    {
        _pilesSerialized = _piles == null || _piles.Count == 0 ? null : _piles.ToArray();
    }

    public void OnAfterDeserialize()
    {
        _piles = _pilesSerialized != null && _pilesSerialized.Length > 0
            ? new List<DropPile>(_pilesSerialized)
            : new List<DropPile>();
    }
}
*/
