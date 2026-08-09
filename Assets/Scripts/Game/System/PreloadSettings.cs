using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PreloadSettings : Singleton<PreloadSettings>{

    private TileAssetTable _tileAssetTable;

    public TileAssetTable GetTileAssetTable(){
        return _tileAssetTable;
    }
    
    public async UniTask LoadSettings(){
        var handle = Addressables.LoadAssetAsync<TileAssetTable>("Config/TileAssetTable");
        await handle;
        _tileAssetTable = Instantiate(handle.Result);
        await UniTask.CompletedTask;
    }
    
}
