using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Panel("Settings", "UI/Settings", "Overlay", EscBehavior =  EscBehavior.OpenAndClose)]
public class SettingsPanel : PanelBase
{
    private bool _IsLoading = false;

    public void OnBtnClick_ExitToMenu()
    {
        OnExitToMenu().Forget();
    }

    private async UniTask OnExitToMenu()
    {
        if (_IsLoading)
            return;
        _IsLoading = true;
        
        if (CombatManager.Instance.IsInBattle)
        {
            CombatManager.Instance.ClearEnemiesTargetingPlayer();
        }
        
        await UIRoot.Instance.LoadingStart(0.25f);
        // 关闭所有界面
        await UIRoot.Instance.CloseAllAsync();
        MapLoader.Instance.ClearScene();
        
        GridIndicatorManager.Instance.ClearAll();
        await Addressables.LoadSceneAsync("Scene/Menu").ToUniTask();
        UIRoot.Instance.CloseMainUI();
        await UIRoot.Instance.OpenStartMenu();
        
        UIRoot.Instance.Hide(Const.KeyUI.SettingPanel);
        
        await UIRoot.Instance.LoadingFinish(0.25f);
        
        _IsLoading = false;
    }

    public void OnBtnClick_QuitSave()
    {
        if (CombatManager.Instance.IsInBattle)
        {
            return;
        }
        Persist.Instance.Save(0);
        OnExitToMenu().Forget();
    }
    
}
