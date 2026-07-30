using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[Panel("StartMenu", "UI/UIStartMenu", "Overlay", MuteGroup = "Overlay", EscBehavior =  EscBehavior.None)]

public class UIStartMenu : PanelBase
{

    [SerializeField] private GameObject _btnContinue;
    
    public override void Open()
    {
        _btnContinue.gameObject.SetActive(Persist.Instance.HasPersistentSlot(0));
        base.Open();
    }

    public void OnBtnClick_Continue()
    {
        OnContinueGame().Forget();
    }
    
    public void OnBtnClick_StartGame()
    {
        OnNewGame().Forget();
    }
    
    private bool _isLoading;
    // 重新开始游戏
    // 需要删除存档
    private async UniTask OnNewGame()
    {
        if (_isLoading)
            return;
        _isLoading = true;
        
        await UIRoot.Instance.LoadingStart();
        // 构建玩家数据
        PlayerManager.Instance.RebuildPersist();
        
        // 加载场景
        await LevelManager.Instance.LoadLevel("Scene/Tutorial");
        await UniTask.DelayFrame(1);
        await this.PublishGlobal(new LevelManager.SceneChangeEvt());
        UIRoot.Instance.Hide("StartMenu");
        UIRoot.Instance.OpenMainUI();
        await UIRoot.Instance.LoadingFinish();
        
        _isLoading = false;
    }
    
    private async UniTask OnContinueGame()
    {
        if (_isLoading)
            return;
        _isLoading = true;
        // 加载数据
        await UIRoot.Instance.LoadingStart();
        Persist.Instance.LoadSlot(0);
        // 构建玩家数据
        PlayerManager.Instance.RebuildPersist();

        await LevelManager.Instance.LoadLevel("Scene/Tutorial");
        await UniTask.DelayFrame(1);
        await this.PublishGlobal(new LevelManager.SceneChangeEvt());
        UIRoot.Instance.Hide("StartMenu");
        UIRoot.Instance.OpenMainUI();
        await UIRoot.Instance.LoadingFinish();

        _isLoading = false;
    }

    private readonly List<string> _languageCodes = new()
    {
        "zh-cn",
        "en",
    };

    [SerializeField] private LocalizedString _locale;
    
    public void OnDropdown_Language(int index)
    {
        var languageCode = _languageCodes[index];
        var locale = LocalizationSettings.AvailableLocales.GetLocale(languageCode);
        
        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
            Debug.Log($"语言已切换为: {languageCode} -> {locale.LocaleName}");
        }
        else
        {
            Debug.LogError($"找不到语言: {languageCode}，请检查 AvailableLocales 中是否已添加该语言");
        }
    }

    public void OnBtnClick_Option()
    {
        //var str = _locale.GetLocalizedString("params");
    }
    
}
