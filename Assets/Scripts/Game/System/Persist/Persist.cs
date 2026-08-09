using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public class Persist : Singleton<Persist>
{
    private SaveData _runtimePlayerData = new();

    public SaveData GetPlayerData()
    {
        return _runtimePlayerData;
    }

    #region Json Settings

#if UNITY_EDITOR
    private const string PersistDir = "Persist";
    private const string PersistNameFormat = "Editor_Persist_{0}.json";
#else
    private const string PersistDir = "Persist";
    private const string PersistNameFormat = "Persist_{0}.json";
#endif

    private const string Password = "BigSmall";

    private static string PersistRoot => Path.Combine(Application.persistentDataPath, PersistDir);
    private static readonly UTF8Encoding Utf8NoBom = new(true);

    private static JsonSerializerSettings SerializerSettings { get; } = CreateSerializerSettings();

    private static JsonSerializerSettings CreateSerializerSettings()
    {
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.None,
            ContractResolver = new DefaultContractResolver()
        };
    }

    private static string GetPersistPath(int slotIndex)
    {
        return Path.Combine(PersistRoot, string.Format(PersistNameFormat, slotIndex));
    }

    #endregion

    public bool Save(int slotIndex)
    {
        try
        {
            var dir = PersistRoot;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var path = GetPersistPath(slotIndex);

            var json = JsonConvert.SerializeObject(_runtimePlayerData, SerializerSettings);
            
#if !UNITY_EDITOR
            json = AesEncryption.EncryptString(json, Password);
#endif
            File.WriteAllText(path, json, Utf8NoBom);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    public bool HasPersistentSlot(int slotIndex)
    {
        var path = GetPersistPath(slotIndex);
        return File.Exists(path);
    }

    public void LoadSlot(int slotIndex)
    {
        var path = GetPersistPath(slotIndex);
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path, Utf8NoBom);
            
#if !UNITY_EDITOR
            json = AesEncryption.DecryptString(json, Password);
#endif
            _runtimePlayerData = JsonConvert.DeserializeObject<SaveData>(json, SerializerSettings);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void ResetSlot(int slotIndex)
    {
        DeleteSlot(slotIndex);
        _runtimePlayerData = new SaveData();
    }

    public void DeleteSlot(int slotIndex)
    {
        try
        {
            var path = GetPersistPath(slotIndex);
            if (!File.Exists(path))
                return; // 文件不存在，没有可删除的
            File.Delete(path);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}