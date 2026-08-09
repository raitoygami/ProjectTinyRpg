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


    private async void Awake()
    {
        try
        {
            Physics.simulationMode = SimulationMode.Update;
#if UNITY_EDITOR
            DOTween.Init(true, false, LogBehaviour.Default);
#else
            DOTween.Init(true, true, LogBehaviour.Default);
#endif
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


            // 初始化输入系统
            InputManager.Instance.Initialized();
            // 音频系统
            var audioManager = new GameObject("AudioManager");
            audioManager.AddComponent<AudioManager>();
            DontDestroyOnLoad(audioManager);

            AudioManager.Instance.Library = preload.AudioLibrary;

            await PreloadSettings.Instance.LoadSettings();
            
            // 加载配置文件
            await ConfigManager.Instance.LoadScriptableTables();
            // Luban 配置
            var configJson = await ConfigManager.Instance.LoadConfigByteBufFromAddressableAsync();
            var tables = new cfg.Tables(file => configJson[file]);
            ConfigManager.Instance.Init(tables);

            // 初始化entity system
            EntityManager.Instance.Init();
            EntityManager.Instance.SetPlayerPrefab(preload.PlayerTemplate);
            EntityManager.Instance.SetEnemyPrefab(preload.EnemyTemplate);
            // 初始化UI
            Instantiate(preload.uiRoot);
            
            // 初始化相机
            CameraManager.Instance.Initialized();
            CameraManager.Instance.Setup(preload.mainCameraPrefab, preload.followCameraPrefab);
            CameraManager.Instance.SetOverlayCamera(UIRoot.Instance.GetUICamera());
            UIRoot.Instance.CloseMainUI();
            
            // 场景相关管理器
            await MapManager.Instance.LoadMapInfo();
            MapLoader.Instance.Initialized();
            
            // In game manager
            PoolManager.Instance.Initialized();
            TurnManager.Instance.Initialized();
            CombatManager.Instance.Initialized();
            FOVManager.Instance.Setup(PreloadSettings.Instance.GetTileAssetTable()); //战争迷雾 
            GridIndicatorManager.Instance.Setup(PreloadSettings.Instance.GetTileAssetTable()); // 技能相关范围指示器
            
            await UIRoot.Instance.FadeIn(0);
            await Addressables.LoadSceneAsync("Scene/Menu").ToUniTask();

            await UIRoot.Instance.OpenStartMenu();

            await UIRoot.Instance.FadeOut();
        }
        catch (Exception e)
        {
            Debug.Log(e.Message);
        }
    }
}