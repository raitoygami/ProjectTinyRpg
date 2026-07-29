using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JSAM;
using Luban;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Game : Singleton<Game>
{
    public class SceneChangeEvt : EventArgs
    {
    }

    public class GameStartEvt : EventArgs
    {
        
    }
    
    private async void Awake()
    {
        try
        {
            {
                Physics.simulationMode = SimulationMode.Update;
                DOTween.logBehaviour = LogBehaviour.Default;
                this.SubscribeGlobal<GameStartEvt>(OnGameStart);
                DontDestroyOnLoad(gameObject);
                
                var preloadHandle = Addressables.LoadAssetAsync<GamePreload>("Settings/GamePreload");
                await preloadHandle;
                if (preloadHandle.Status != AsyncOperationStatus.Succeeded || preloadHandle.Result == null)
                {
                    Debug.LogError("Addressable 加载失败: Settings/GamePreload");
                    return;
                }

                var preload = preloadHandle.Result;
                
                Context.Instance.Initialized();
                
                // 初始化相机
                CameraManager.Instance.Initialized();
                CameraManager.Instance.Setup(preload.mainCameraPrefab, preload.followCameraPrefab);
                
                // 初始化输入系统
                InputManager.Instance.Initialized();
                // 音频系统
                var audioManager = new GameObject("AudioManager");
                audioManager.AddComponent<AudioManager>();
                DontDestroyOnLoad(audioManager);

                AudioManager.Instance.Library = preload.AudioLibrary;
                
                await PreloadSettings.Instance.LoadSettings();
                // 加载配置文件
                var configJson = await ConfigManager.Instance.LoadConfigByteBufFromAddressableAsync();
                var tables = new cfg.Tables(file => configJson[file]);
                ConfigManager.Instance.Init(tables);
                
                // 初始化entity system
                EntityManager.Instance.Init();
                EntityManager.Instance.SetPlayerPrefab(preload.PlayerTemplate);
                EntityManager.Instance.SetEnemyPrefab(preload.EnemyTemplate);

                TurnManager.Instance.Initialized();
                
                EquipmentManager.Instance.Initialized();

                TileSelector.Instance.Setup(PreloadSettings.Instance.NavigationSetting());
                
                // 初始化UI
                Instantiate(preload.uiRoot);
                CameraManager.Instance.SetOverlayCamera(UIRoot.Instance.GetUICamera());
                UIRoot.Instance.CloseMainUI();
                
                LevelManager.Instance.Initialized();
                
                await UIRoot.Instance.FadeIn(0);
                await Addressables.LoadSceneAsync("Scene/Menu").ToUniTask();

                await UIRoot.Instance.OpenStartMenu();
                
                await UIRoot.Instance.FadeOut();
                
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    private UniTask OnGameStart(GameStartEvt arg)
    {
        UIRoot.Instance.CloseMainUI();
        return UniTask.CompletedTask;
    }   
    
    private bool _isExiting = false;
    public async UniTask ExitToTitle()
    {
        if (_isExiting)
            return;

        if (InputManager.HasInstance())
            InputManager.Instance.DisableInput();
        
        _isExiting = true;
        await UIRoot.Instance.LoadingStart();
        // 关闭主界面
        UIRoot.Instance.CloseMainUI();

        if (EntityManager.HasInstance())
        {
            EntityManager.DestroyAll();
        }

        Context.Instance.SetPlayer(null);        
        CameraManager.Instance.ClearFollowTarget();
        
        await Addressables.LoadSceneAsync("Scene/Title.unity").ToUniTask();
        await UIRoot.Instance.LoadingFinish();
        
        if (InputManager.HasInstance())
            InputManager.Instance.EnableInput();

        _isExiting = false;
    }
    
    public async UniTask NewGame()
    {
        await UniTask.Delay(1);
    }
    
}
