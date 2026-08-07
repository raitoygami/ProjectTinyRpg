using System;
using System.Collections.Generic;
using cfg;
using Cysharp.Threading.Tasks;
using Luban;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 全局 Luban 配置表入口；在 <see cref="Game"/> 加载完 JSON 后 <see cref="Init"/> 一次。
/// </summary>
public partial class ConfigManager : Singleton<ConfigManager>
{
    private Tables _tables;

    public void Init(Tables tables) => _tables = tables;

    /// <summary>当前配置表；未 Init 前为 null。</summary>
    public Tables Tables => _tables;

    // 放在类的内部，作为静态只读字段
    private static readonly string[] ByteFileNames = new[]
    {
        "data_drop",
        "data_entities",
        "data_item",
        "data_equip",
        "data_ability",
        "data_levelup"
    };
  
    public async UniTask<Dictionary<string, ByteBuf>> LoadConfigByteBufFromAddressableAsync()
    {
        var map = new Dictionary<string, ByteBuf>(ByteFileNames.Length);

        foreach (var n in ByteFileNames)
        {
            var address = $"Config/{n.ToLower()}.bytes";
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"Addressables 加载配置失败: {address}");
                Addressables.Release(handle);
                throw new InvalidOperationException($"Failed to load config: {address}");
            }

            map[n] = new ByteBuf(handle.Result.bytes);
            Addressables.Release(handle);
        }

        return map;
    }
    
}
