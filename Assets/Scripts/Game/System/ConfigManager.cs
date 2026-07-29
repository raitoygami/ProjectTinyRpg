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
public class ConfigManager : Singleton<ConfigManager>
{
    private Tables _tables;

    public void Init(Tables tables) => _tables = tables;

    /// <summary>当前配置表；未 Init 前为 null。</summary>
    public Tables Tables => _tables;

    public t_Item GetItem(int itemID)
    {
        return Tables?.DataItem.GetOrDefault(itemID);
    }

    /// <summary>
    /// 先查装备表再查道具表（共用 id 时以装备为准）；用于俄罗斯方块背包等需 <see cref="t_ItemBase.X"/> / <see cref="t_ItemBase.Y"/> 的配置。
    /// </summary>
    public t_ItemBase GetItemBase(int itemId)
    {
        if (Tables == null)
            return null;
        var equip = Tables.DataEquip.GetOrDefault(itemId);
        if (equip != null)
            return equip;
        return Tables.DataItem.GetOrDefault(itemId);
    }

    public t_Drop GetDrop(int dropID)
    {
        return Tables?.DataDrop.GetOrDefault(dropID);
    }
    
    /// <summary>
    /// 通过 Addressables 异步加载 Luban 导出的 .bytes 二进制文件
    /// </summary>
    public async UniTask<Dictionary<string, ByteBuf>> LoadConfigByteBufFromAddressableAsync()
    {
        var names = new[] { "data_drop", "data_entitys", "data_item", "data_equip", "data_ai" };
        var map = new Dictionary<string, ByteBuf>(names.Length);

        foreach (var n in names)
        {
            var address = $"Config/{n}.bytes";   // 注意后缀改为 .bytes
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
        
            await handle.Task;   // 推荐使用 .Task

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"Addressables 加载配置失败: {address}");
                Addressables.Release(handle);
                throw new InvalidOperationException($"Failed to load config: {address}");
            }

            // 关键修改：把 TextAsset 的 bytes 包装成 ByteBuf
            map[n] = new ByteBuf(handle.Result.bytes);

            Addressables.Release(handle);
        }

        return map;
    }
    
}
