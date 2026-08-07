using System;
using Cysharp.Threading.Tasks;
using Luban;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public partial class ConfigManager
{
    
    public ScriptableContainer ScriptableContainer;
    
    public async UniTask LoadScriptableTables()
    {
        const string address = "Config/ScriptableContainer";
        var handle = Addressables.LoadAssetAsync<ScriptableContainer>(address);
        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogError($"Addressables 加载配置失败: {address}");
            Addressables.Release(handle);
            throw new InvalidOperationException($"Failed to load config: {address}");
        }

        ScriptableContainer = Instantiate(handle.Result);
        Addressables.Release(handle);
    }

}
