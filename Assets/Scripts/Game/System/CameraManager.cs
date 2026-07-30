using UnityEngine;
using Unity.Cinemachine;                      // 注意：新命名空間
using UnityEngine.Rendering.Universal;


/// <summary>
/// 相機管理：從預製體生成主相機（不隨場景銷毀）、Overlay 相機供 UI 使用、
/// 主相機綁定 CinemachineBrain，並提供帶 PositionComposer 的 CinemachineCamera 及 Follow 玩家接口。
/// </summary>
public class CameraManager : Singleton<CameraManager>
{
    private Camera _mainCamera;
    private Camera _overlayCamera;
    private CinemachineBrain _brain;
    private CinemachineCamera _followCam;                   // 原 _followVcam
    private CinemachinePositionComposer _positionComposer;  // 原 _framingTransposer

    public Camera MainCamera => _mainCamera;
    public CinemachineBrain Brain => _brain;
    public CinemachineCamera FollowCamera => _followCam;

    public void Setup(GameObject cameraPrefab, GameObject followCameraPrefab)
    {
        if (cameraPrefab == null)
        {
            Debug.LogError("CameraManager: 未指定 Main Camera 預製體。");
            return;
        }

        var mainGo = Instantiate(cameraPrefab);
        mainGo.name = cameraPrefab.name;
        _mainCamera = mainGo.GetComponent<Camera>();
        _mainCamera.transform.position = Vector3.up;
        _followCam = Instantiate(followCameraPrefab).GetComponent<CinemachineCamera>();
        if (_mainCamera == null)
            _mainCamera = mainGo.GetComponentInChildren<Camera>();
        if (_mainCamera == null)
        {
            Debug.LogError("CameraManager: 主相機預製體上無 Camera 組件。");
            return;
        }

        // orthographicSize 僅在 Orthographic 模式才有意義，這裡保留原邏輯
        _mainCamera.orthographicSize = 7.5f;
        _followCam.Lens.OrthographicSize = 7.5f;
        _brain = _mainCamera.GetComponent<CinemachineBrain>();
        if (_brain == null)
            _brain = _mainCamera.gameObject.AddComponent<CinemachineBrain>();
        
        DontDestroyOnLoad(mainGo);
        DontDestroyOnLoad(_followCam.gameObject);
    }

    /// <summary>設置 Overlay 相機，供 UI Canvas（如 Screen Space - Camera / Overlay）使用。</summary>
    public void SetOverlayCamera(Camera overlay)
    {
        _overlayCamera = overlay;
        var baseCameraData = _mainCamera.GetUniversalAdditionalCameraData();
        
        if (_overlayCamera != null && !baseCameraData.cameraStack.Contains(_overlayCamera))
        {
            baseCameraData.cameraStack.Add(_overlayCamera);
        }
    }

    /// <summary>設置 Cinemachine 相機跟隨目標（如玩家）。</summary>
    public void SetFollowTarget(Transform target)
    {
        
        if (_followCam != null)
        {
            _followCam.transform.position = target.position + Vector3.up * 25f;
            _followCam.Target.TrackingTarget = target;   // 新版屬性：TrackingTarget
            // 如果你原本希望 LookAt 也跟隨，可額外設定：
            // _followCam.LookAtTarget = target;
            _followCam.Target.LookAtTarget = target;
        }
    }

    /// <summary>清除跟隨目標。</summary>
    public void ClearFollowTarget()
    {
        if (_followCam != null)
        {
            _followCam.Target.LookAtTarget = null;
            _followCam.Target.TrackingTarget = null;
        }
            
    }

    /// <summary>獲取當前用於 Follow 的 Position Composer，可進一步調整參數。</summary>
    public CinemachinePositionComposer GetPositionComposer()
    {
        return _positionComposer;
        // 或者動態獲取：return _followCam ? _followCam.GetComponent<CinemachinePositionComposer>() : null;
    }
}