using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using JSAM;
using SimpleJSON;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

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
            //try
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
                
                // 初始化相机
                CameraManager.Instance.Initialized();
                CameraManager.Instance.Setup(preload.mainCameraPrefab, preload.followCameraPrefab);
                
                // 初始化输入系统
                InputManager.Instance.Initialized();
                // 音频系统
                var audioManager = new GameObject("AudioManager");
                audioManager.AddComponent<AudioManager>();
                DontDestroyOnLoad(audioManager);

                await PreloadSettings.Instance.LoadSettings();
                // 加载配置文件
                var configJson = await LoadConfigJsonFromAddressablesAsync();
                var tables = new cfg.Tables(file => configJson[file]);
                ConfigManager.Instance.Init(tables);
                
                // 初始化entity system
                EntityManager.Instance.Init();
                EntityManager.Instance.SetPlayerPrefab(preload.PlayerTemplate);
                EntityManager.Instance.SetEnemyPrefab(preload.EnemyTemplate);

                TurnManager.Instance.Initialized();

                InventoryManager.Instance.Initialized();
                EquipmentManager.Instance.Initialized();

                TileSelector.Instance.Setup(PreloadSettings.Instance.NavigationSetting());

                TetrisHandle.Instance.Initialized();

                Context.Instance.Initialized();
                
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

    /// <summary>
    /// 通过 Addressables 加载 Luban 导出的 JSON（与 Tables 中 loader 文件名一致：data_drop、data_entitys、data_item、data_equip、data_ai）。
    /// </summary>
    private async UniTask<Dictionary<string, JSONNode>> LoadConfigJsonFromAddressablesAsync()
    {
        var names = new[] { "data_drop", "data_entitys", "data_item", "data_equip", "data_ai" };
        var map = new Dictionary<string, JSONNode>(names.Length);
        foreach (var n in names)
        {
            var address = $"Config/{n}.json";
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            await handle;
            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                Debug.LogError($"Addressables 加载配置失败: {address}");
                throw new InvalidOperationException($"Failed to load config: {address}");
            }
            map[n] = JSON.Parse(handle.Result.text);
            Addressables.Release(handle);
        }
        return map;
    }
    
    private bool _isLoading;
    // Update is called once per frame
    public async UniTask LoadGame()
    {
        if (_isLoading)
            return;
        
        _isLoading = true;
        await UIRoot.Instance.LoadingStart();
        
        UIRoot.Instance.OpenMainUI();
        
        Persist.Instance.Load(0);
        
        await Addressables.LoadSceneAsync("Scene/Turn1.unity").ToUniTask();

        // 加载地图 根据存档
        PathFinder.Instance.InitCells(-25, -25, 51, 51);

        // 加载玩家
        var p = EntityManager.Instance.CreatePlayer(Vector3.right, 100001);
        Context.Instance.SetPlayer(p);
        CameraManager.Instance.SetFollowTarget(p.transform);

        await UniTask.DelayFrame(1);
        
        await this.PublishGlobal(new SceneChangeEvt());
        
        await UIRoot.Instance.LoadingFinish();
        
        _isLoading = false;
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
