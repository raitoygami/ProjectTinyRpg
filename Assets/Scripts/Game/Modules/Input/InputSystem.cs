using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : Singleton<InputSystem>
{
    private InputMapping m_InputMapping;
    private PlayerInput m_PlayerInput;

    // ==================== Rebinding 相关 ====================
    private InputActionRebindingExtensions.RebindingOperation currentRebindOperation;
    private const string REBIND_SAVE_KEY = "InputSystem_Rebinds";

    public class MouseClickEvt : EventArgs
    {
        public int mouseIndex;
    }

    private MouseClickEvt m_MouseClickEvt;

    public class WASDEvt : EventArgs
    {
        public Vector2 Direction;
    }

    private WASDEvt m_WASDEvt;

    public class PointerMoveEvt : EventArgs
    {
        public Vector2 Position;
    }

    private PointerMoveEvt m_PointerMoveEvt;

    /// <summary>背包快捷键按下（与 <see cref="InputMapping.PlayerInput.Inventory" /> 的 performed 对应）。</summary>
    public class InventoryEvt : EventArgs
    {
    }

    private readonly InventoryEvt m_InventoryEvt = new();

    public class OverworldEvt : EventArgs
    {
    }

    private readonly OverworldEvt m_OverworldEvt;

    public class EscPressedEvt : EventArgs
    {
    }

    private readonly EscPressedEvt m_EscPressedEvt = new();

    public override void Initialized()
    {
        m_InputMapping = new InputMapping();
        m_PlayerInput = gameObject.AddComponent<PlayerInput>();
        m_PlayerInput.actions = m_InputMapping.asset;
        m_PlayerInput.neverAutoSwitchControlSchemes = false;

        m_WASDEvt = new WASDEvt();
        m_MouseClickEvt = new MouseClickEvt();
        m_PointerMoveEvt = new PointerMoveEvt();

        m_InputMapping.PlayerInput.LeftMouseClick.performed += OnLeftMouseClickPerformed;
        m_InputMapping.PlayerInput.RightMouseClick.performed += OnRightMouseClickPerformed;
        m_InputMapping.PlayerInput.Movement.performed += OnMovementPerformed;
        m_InputMapping.PlayerInput.Movement.canceled += OnMovementCanceled;
        m_InputMapping.PlayerInput.PointerPosition.performed += OnPointerPositionPerformed;
        m_InputMapping.PlayerInput.Inventory.performed += OnInventoryPerformed;
        m_InputMapping.PlayerInput.Overworld.performed += OnOverworldPerformed;
        m_InputMapping.PlayerInput.Esc.performed += OnEscPerformed;


        m_InputMapping.PlayerInput.Enable();
        LoadBindings(); // 启动时加载已保存的改键
    }
    
    /// <summary>
    ///     开始改键（支持复合绑定 + 键鼠/手柄）
    /// </summary>
    public void StartRebinding(string actionName, int bindingIndex = 0, Action onComplete = null)
    {
        var action = m_InputMapping.asset.FindAction(actionName);
        if (action == null)
        {
            Debug.LogError($"[InputSystem] 未找到 Action: {actionName}");
            return;
        }

        currentRebindOperation?.Cancel();
        action.Disable();

        currentRebindOperation = action.PerformInteractiveRebinding(bindingIndex)
            // 1.19.0 常用配置
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Keyboard>/escape")
            .WithControlsExcluding("<Gamepad>/start")
            .WithControlsExcluding("<Gamepad>/select")
            .OnMatchWaitForAnother(0.1f) // 防止连点
            .OnComplete(operation =>
            {
                action.Enable();
                SaveBindings();
                onComplete?.Invoke();
                Debug.Log($"[Rebind] {actionName} → {action.GetBindingDisplayString(bindingIndex)}");
                currentRebindOperation = null;
            })
            .OnCancel(operation =>
            {
                action.Enable();
                currentRebindOperation = null;
            })
            .Start();
    }

    public void SaveBindings()
    {
        var json = m_InputMapping.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(REBIND_SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    public void LoadBindings()
    {
        var json = PlayerPrefs.GetString(REBIND_SAVE_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            m_InputMapping.asset.LoadBindingOverridesFromJson(json);
            Debug.Log("[InputSystem] 按键配置加载成功");
        }
    }

    /// <summary>
    ///     还原所有按键为项目默认值
    /// </summary>
    public void ResetToDefaultBindings()
    {
        currentRebindOperation?.Cancel();
        m_InputMapping.asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(REBIND_SAVE_KEY);
        PlayerPrefs.Save();

        Debug.Log("[InputSystem] 已还原所有按键为默认");
    }

    /// <summary>
    ///     获取当前绑定显示文字（推荐在UI上调用）
    /// </summary>
    public string GetBindingDisplay(string actionName, int bindingIndex = 0)
    {
        var action = m_InputMapping.asset.FindAction(actionName);
        return action?.GetBindingDisplayString(bindingIndex) ?? "未绑定";
    }

    // ====================== 控制方案切换 ======================

    /// <summary>
    ///     切换控制方案（键鼠 / 手柄）
    /// </summary>
    public void SwitchControlScheme(string schemeName)
    {
        if (m_PlayerInput == null) return;

        m_PlayerInput.SwitchCurrentControlScheme(schemeName);
        Debug.Log($"[InputSystem] 已切换控制方案 → {schemeName}");
    }

    public void SwitchToKeyboardMouse()
    {
        SwitchControlScheme("Keyboard&Mouse");
    }

    public void SwitchToGamepad()
    {
        SwitchControlScheme("Gamepad");
    }

    protected override void OnRelease()
    {
        currentRebindOperation?.Cancel();
    }

    // 事件

    private void OnPointerPositionPerformed(InputAction.CallbackContext context)
    {
        m_PointerMoveEvt.Position = context.ReadValue<Vector2>();
        this.Publish(m_PointerMoveEvt);
    }

    private void OnLeftMouseClickPerformed(InputAction.CallbackContext _)
    {
        m_MouseClickEvt.mouseIndex = 0;
        this.Publish(m_MouseClickEvt);
    }

    private void OnRightMouseClickPerformed(InputAction.CallbackContext _)
    {
        m_MouseClickEvt.mouseIndex = 1;
        this.Publish(m_MouseClickEvt);
    }

    private void OnMovementPerformed(InputAction.CallbackContext context)
    {
        m_WASDEvt.Direction = context.ReadValue<Vector2>();
        this.Publish(m_WASDEvt);
    }

    private void OnMovementCanceled(InputAction.CallbackContext _)
    {
        m_WASDEvt.Direction = Vector2.zero;
        this.Publish(m_WASDEvt);
    }

    private void OnInventoryPerformed(InputAction.CallbackContext _)
    {
        this.Publish(m_InventoryEvt);
    }


    private void OnOverworldPerformed(InputAction.CallbackContext obj)
    {
        this.Publish(m_OverworldEvt);
    }

    private void OnEscPerformed(InputAction.CallbackContext obj)
    {
        this.Publish(m_EscPressedEvt);
    }


    public void EnableInput()
    {
        m_InputMapping.PlayerInput.Enable();
    }

    /// <summary>
    /// 屏蔽整个输入系统（切换场景时推荐调用）
    /// </summary>
    public void DisableInput()
    {
        m_InputMapping.PlayerInput.Disable();
    }
    
    public void MouseDisable()
    {
        m_InputMapping.PlayerInput.LeftMouseClick.Disable();
        m_InputMapping.PlayerInput.RightMouseClick.Disable();
        m_InputMapping.PlayerInput.PointerPosition.Disable();
    }

    public void MouseEnable()
    {
        m_InputMapping.PlayerInput.LeftMouseClick.Enable();
        m_InputMapping.PlayerInput.RightMouseClick.Enable();
        m_InputMapping.PlayerInput.PointerPosition.Enable();
    }

    public InputMapping GetInputMapping()
    {
        return m_InputMapping;
    }
}