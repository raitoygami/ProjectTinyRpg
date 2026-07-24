using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PreloadSettings : Singleton<PreloadSettings>{

    private NavigationSettings m_NavigationSettings;

    public NavigationSettings NavigationSetting(){
        return m_NavigationSettings;
    }
    
    public async UniTask LoadSettings(){
        var handle = Addressables.LoadAssetAsync<NavigationSettings>("Settings/Navigation");
        await handle;
        m_NavigationSettings = Instantiate(handle.Result);
        await UniTask.CompletedTask;
    }
    
}
