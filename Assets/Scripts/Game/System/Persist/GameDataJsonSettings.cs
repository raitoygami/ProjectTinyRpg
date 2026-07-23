using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>
/// <see cref="GameData"/> 存档用 Newtonsoft.Json 设置（多态 <see cref="QuestGoal"/> 等依赖 <see cref="TypeNameHandling.Auto"/>）。
/// </summary>
public static class GameDataJsonSettings
{
    static readonly JsonSerializerSettings Cached = Create();

    public static JsonSerializerSettings Instance => Cached;

    static JsonSerializerSettings Create()
    {
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.None,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };
    }
}
