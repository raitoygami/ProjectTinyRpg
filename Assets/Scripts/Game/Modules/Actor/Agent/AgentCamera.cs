/*using UnityEngine;
using Unity.Cinemachine;  // 注意命名空間改變！

public class AgentCamera : MonoBehaviour
{
    private CinemachineCamera m_CmCamera;           // 原 VirtualCamera → CinemachineCamera
    private CinemachinePositionComposer m_PositionComposer;  // 原 FramingTransposer
    // private CinemachineOrbitalFollow m_OrbitalFollow;      // 如果之後要用軌道
    // private CinemachineConfiner3D m_Confiner3D;

    private void Awake()
    {
        // 建立新的 Cinemachine Camera（不再叫 Virtual Camera）
        var camObj = new GameObject("Player Cm Camera");
        m_CmCamera = camObj.AddComponent<CinemachineCamera>();

        // 推薦：直接添加 Position Composer（類似原 FramingTransposer）
        m_PositionComposer = m_CmCamera.gameObject.AddComponent<CinemachinePositionComposer>();

        // 常用參數對應（大部分名字相同或很接近）
        m_PositionComposer.TargetOffset = Vector3.zero;               // 原 m_TrackedObjectOffset
        m_PositionComposer.CameraDistance = 30f;                      // 原 m_CameraDistance
        /*m_PositionComposer.Lookahead = new LookaheadSettings()
        m_PositionComposer.LookaheadSmoothing = 0f;
        m_PositionComposer.HorizontalDamping = 0f;                    // 原 XDamping
        m_PositionComposer.VerticalDamping = 0f;                      // 原 YDamping
        m_PositionComposer.DepthDamping = 0f;                         // 原 ZDamping#1#

        // 鏡頭設置（Lens）
        m_CmCamera.Lens.FieldOfView = 20f;
        m_CmCamera.Lens.ModeOverride = LensSettings.OverrideModes.Perspective;
        // m_CmCamera.Lens.OrthographicSize = ... （如果你之後切正交投影再開啟）
        m_CmCamera.Lens.NearClipPlane = 0.01f;
        m_CmCamera.Lens.FarClipPlane = 100f;

        // 初始旋轉（45度俯角）
        camObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

        // 追蹤目標
        m_CmCamera.Follow = transform;   // 跟隨本物件（通常是玩家）
        // m_CmCamera.LookAt = transform;   // 如果你想要盯著看，可以加上這行

        m_CmCamera.Priority = 15;

        // 建議：不要用 DontDestroyOnLoad，除非真的跨場景需要
        // DontDestroyOnLoad(camObj);

        // ──────────────────────────────────────────────
        // 如果你原本想用 Confiner（3D 邊界限制）
        // ──────────────────────────────────────────────
        /*
        var boundsObj = GameObject.Find("CameraBounds");
        if (boundsObj != null)
        {
            var collider = boundsObj.GetComponent<Collider>();
            if (collider != null)
            {
                m_Confiner3D = m_CmCamera.gameObject.AddComponent<CinemachineConfiner3D>();
                m_Confiner3D.BoundingVolume = collider;
                // m_Confiner3D.ConfineMode = ... （根據需求）
            }
        }
        #1#
    }

    // 可選：如果你之後想加上手動軌道環繞（原 OrbitalTransposer）
    // private void Start()
    // {
    //     m_OrbitalFollow = m_CmCamera.gameObject.AddComponent<CinemachineOrbitalFollow>();
    //     // 再設定各種參數...
    // }
}*/