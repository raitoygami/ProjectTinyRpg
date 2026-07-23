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
        UIRoot.Instance.CloseMainUI();
        
        await Addressables.LoadSceneAsync("Scene/Menu").ToUniTask();
        await UIRoot.Instance.OpenStartMenu();
        
        UIRoot.Instance.Hide(Const.KeyUI.SettingPanel);
        await UIRoot.Instance.LoadingFinish(0.25f);
        
        _IsLoading = false;
    }
    
}
