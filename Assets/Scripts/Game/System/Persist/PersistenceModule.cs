using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
///     数据本地持久化模块：使用 <see cref="Newtonsoft.Json" /> 读写 <see cref="GameData" />（UTF-8 文本 JSON，不兼容旧 Odin 字节档）。
///     Unity 的 <see cref="ISerializationCallbackReceiver" /> 不会随 Json 自动触发，故在写/读前后手动调用
///     <see cref="GameData.OnBeforeSerialize" /> / <see cref="GameData.OnAfterDeserialize" />。
///     每个槽位可带有 <see cref="SaveSlotSnapshot" />（<c>*.snapshot.json</c>），记录存档时的回合与游戏内时间供 UI 展示。
/// </summary>
public class PersistenceModule : Singleton<PersistenceModule>
{
    [Tooltip("存档槽位数量")] [SerializeField] private int _slotCount = 3;

    private const string SaveDirName = "Saves";
    private const string SaveFileNameFormat = "slot_{0}.json";
    private const string SnapshotFileNameFormat = "slot_{0}.snapshot.json";

    private static readonly UTF8Encoding Utf8NoBom = new(false);

    private static readonly JsonSerializerSettings SnapshotJsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    private static string SaveRoot => Path.Combine(Application.persistentDataPath, SaveDirName);

    private static string GetSlotPath(int slotIndex)
    {
        return Path.Combine(SaveRoot, string.Format(SaveFileNameFormat, slotIndex));
    }

    private static string GetSnapshotPath(int slotIndex)
    {
        return Path.Combine(SaveRoot, string.Format(SnapshotFileNameFormat, slotIndex));
    }

    public int SlotCount => _slotCount;

    public GameData GetRuntimeData()
    {
        return _runtimeData;
    }

    private GameData _runtimeData = new();

    public bool HasSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotCount) return false;
        return File.Exists(GetSlotPath(slotIndex));
    }

    /// <summary>
    ///     读取槽位快照（游戏内时间与回合）；若无快照文件或解析失败返回 <c>false</c>（旧档或未成功写过快照）。
    /// </summary>
    public bool TryGetSlotSnapshot(int slotIndex, out SaveSlotSnapshot snapshot)
    {
        snapshot = null;
        if (slotIndex < 0 || slotIndex >= _slotCount) return false;

        var path = GetSnapshotPath(slotIndex);
        if (!File.Exists(path)) return false;

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            snapshot = JsonConvert.DeserializeObject<SaveSlotSnapshot>(json, SnapshotJsonSettings);
            return snapshot != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Persistence] 读取快照失败 slot={slotIndex}: {e.Message}");
            return false;
        }
    }

    public bool Save(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotCount) return false;

        try
        {
            var dir = SaveRoot;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var path = GetSlotPath(slotIndex);

            _runtimeData.OnBeforeSerialize();
            var json = JsonConvert.SerializeObject(_runtimeData, GameDataJsonSettings.Instance);
            File.WriteAllText(path, json, Utf8NoBom);

            WriteSlotSnapshot(slotIndex);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    private void WriteSlotSnapshot(int slotIndex)
    {
        try
        {
            var round = 0;
            if (TurnManager.HasInstance())
                round = TurnManager.Instance.CurrentGameTime;

            var minutes = GameTimeConverter.TurnRoundToGameMinutes(round);
            var snap = new SaveSlotSnapshot
            {
                turnRound = round,
                gameMinutes = minutes,
                savedAtUtcIso = DateTime.UtcNow.ToString("o")
            };

            var snapPath = GetSnapshotPath(slotIndex);
            var snapJson = JsonConvert.SerializeObject(snap, SnapshotJsonSettings);
            File.WriteAllText(snapPath, snapJson, Utf8NoBom);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Persistence] 写入快照失败 slot={slotIndex}: {e.Message}");
        }
    }

    public GameData Load(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotCount) return null;
        var path = GetSlotPath(slotIndex);
        if (!File.Exists(path)) return null;
        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var data = JsonConvert.DeserializeObject<GameData>(json, GameDataJsonSettings.Instance);
            if (data == null)
            {
                Debug.LogError("[Persistence] 反序列化结果不是 GameData: null");
                return null;
            }

            data.OnAfterDeserialize();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }

    public bool LoadAndApply(int slotIndex)
    {
        var data = Load(slotIndex);

        if (data == null) return false;
        _runtimeData = data;
        return true;
    }

    public bool DeleteSave(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotCount) return false;
        var ok = true;

        var path = GetSlotPath(slotIndex);
        if (File.Exists(path))
            try
            {
                File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ok = false;
            }

        var snap = GetSnapshotPath(slotIndex);
        if (File.Exists(snap))
            try
            {
                File.Delete(snap);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                ok = false;
            }

        return ok;
    }
}