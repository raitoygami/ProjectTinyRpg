using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Panel("Settings", "UI/Settings", "Overlay", EscBehavior =  EscBehavior.OpenAndClose)]
public class SettingsPanel : PanelBase
{
    private bool _IsLoading = false;
    
    public void OnBtnClick_ExitToDesktop()
    {
        
    }

    public void OnBtnClick_ExitToMenu()
    {
        OnExitToMenu().Forget();
    }

    private async UniTask OnExitToMenu()
    {
        if (_IsLoading)
            return;
        _IsLoading = true;
        
        await UIRoot.Instance.LoadingStart(0.25f);
        
        LevelManager.Instance.ClearScene();
        
        await Addressables.LoadSceneAsync("Scene/Menu").ToUniTask();
        UIRoot.Instance.CloseMainUI();
        await UIRoot.Instance.OpenStartMenu();
        Debug.Log(TurnManager.Instance.GetEntityCount());
        UIRoot.Instance.Hide(Const.KeyUI.SettingPanel);
        
        await UIRoot.Instance.LoadingFinish(0.25f);
        
        _IsLoading = false;
    }
    
}
