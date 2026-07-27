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
    
    private void Awake()
    {
        _btnContinue.gameObject.SetActive(Persist.Instance.HasPersistentSlot(0));
    }

    public void OnBtnClick_Continue()
    {
        OnContinueGame().Forget();
    }
    
    public void OnBtnClick_StartGame()
    {
        OnStartGame().Forget();
    }
    
    private bool _isLoading;
    
    private async UniTask OnStartGame()
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
        
        await this.PublishGlobal(new Game.SceneChangeEvt());
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
        Persist.Instance.Load(0);
        // 构建玩家数据
        PlayerManager.Instance.RebuildPersist();

        await LevelManager.Instance.LoadLevel("Scene/Tutorial");

        await UniTask.DelayFrame(1);
        
        await this.PublishGlobal(new Game.SceneChangeEvt());
        
        await UIRoot.Instance.LoadingFinish();
        
        Debug.Log(Context.Instance.PlayerInst.GridPosition);
        
        _isLoading = false;
    }

    private List<string> _languageCodes = new List<string>()
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
