using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 统一存档根数据。各模块为顶层可序列化字段（如 <c>dropScenes</c>、<c>quests</c>）。
/// 由 <see cref="PersistenceModule"/> 使用 Newtonsoft.Json 读写；<see cref="OnBeforeSerialize"/> / <see cref="OnAfterDeserialize"/> 由 Persistence 在序列化前后手动调用。
/// 各 partial 通过 <see cref="RegisterSerializationCallbacks"/> 在静态字段初始化时注册，勿在本文件中逐模块堆叠逻辑。
/// </summary>
[Serializable]
public partial class GameData : ISerializationCallbackReceiver
{
    /// <summary>
    /// partial 之间静态字段初始化顺序未定义，勿在声明处 <c>= new()</c> 后假定早于其他 partial 的注册字段；
    /// 统一惰性创建，避免 <see cref="RegisterSerializationCallbacks"/> 在列表尚未初始化时执行。
    /// </summary>
    private static List<Action<GameData>> BeforeSerializeHandlers;
    private static List<Action<GameData>> AfterDeserializeHandlers;

    private static void EnsureHandlerLists()
    {
        BeforeSerializeHandlers ??= new List<Action<GameData>>();
        AfterDeserializeHandlers ??= new List<Action<GameData>>();
    }

    /// <summary>
    /// 由 <see cref="GameData"/> 的各 partial 在静态字段初始化时调用：存档写出前 / 读档反序列化后对本实例执行的步骤。
    /// </summary>
    public static void RegisterSerializationCallbacks(Action<GameData> beforeSerialize, Action<GameData> afterDeserialize)
    {
        EnsureHandlerLists();
        if (beforeSerialize != null)
            BeforeSerializeHandlers.Add(beforeSerialize);
        if (afterDeserialize != null)
            AfterDeserializeHandlers.Add(afterDeserialize);
    }

    public void OnBeforeSerialize()
    {
        EnsureHandlerLists();
        for (var i = 0; i < BeforeSerializeHandlers.Count; i++)
            BeforeSerializeHandlers[i](this);
    }

    public void OnAfterDeserialize()
    {
        EnsureHandlerLists();
        for (var i = 0; i < AfterDeserializeHandlers.Count; i++)
            AfterDeserializeHandlers[i](this);
    }
}
