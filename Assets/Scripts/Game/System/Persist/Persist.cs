using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public class Persist : Singleton<Persist>
{
    private GameState _runtimeState = new();

    public GameState GetState()
    {
        return _runtimeState;
    }

    #region Json Settings

#if UNITY_EDITOR
    private const string PersistDir = "Persist";
    private const string PersistNameFormat = "Editor_Persist_{0}.json";
#else
    private const string PersistDir = "Persist";
    private const string PersistNameFormat = "Persist_{0}.json";
#endif

    private const byte Key = 0xAB; // 可换成多字节序列

    public static string EncryptDecrypt(string input)
    {
        var result = input.ToCharArray();
        for (var i = 0; i < result.Length; i++)
            result[i] = (char) (result[i] ^ Key);
        return new string(result);
    }

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
            ContractResolver = new CamelCasePropertyNamesContractResolver()
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

            var json = JsonConvert.SerializeObject(_runtimeState, SerializerSettings);
#if !UNITY_EDITOR
            json = EncryptDecrypt(json);
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
    
    public void Load(int slotIndex)
    {
        var path = GetPersistPath(slotIndex);
        if (!File.Exists(path)) return;
        try
        {
            var json = File.ReadAllText(path, Utf8NoBom);
#if !UNITY_EDITOR
            json = EncryptDecrypt(json);
#endif
            _runtimeState = JsonConvert.DeserializeObject<GameState>(json, SerializerSettings);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}