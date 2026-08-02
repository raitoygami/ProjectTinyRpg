using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : Singleton<InputManager>
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

    public class StatsEvt : EventArgs
    {
        
    }
    private readonly StatsEvt m_StatsEvt = new();
    
    public class SkipEvt : EventArgs
    {
    }

    private readonly SkipEvt _skipEvt = new();

    public class EscPressedEvt : EventArgs
    {
    }

    private readonly EscPressedEvt m_EscPressedEvt = new();

    public class SwitchEvt : EventArgs
    {
    }

    private readonly SwitchEvt m_SwitchEvt = new();

    public class HotkeyEvt : EventArgs
    {
        public int Index;
    }

    private readonly HotkeyEvt m_HotkeyEvt = new();

    public class QuickBarEvt : EventArgs
    {
        public int Index;
    }
    private readonly QuickBarEvt m_QuickBarEvt = new();
    
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
        m_InputMapping.PlayerInput.Stats.performed += OnStatsPerformed;
        m_InputMapping.PlayerInput.Skip.performed += OnSkipPerformed;
        m_InputMapping.PlayerInput.Esc.performed += OnEscPerformed;
        // action panel hot key
        m_InputMapping.PlayerInput.Switch.performed += OnSwitchPerformed;
        
        m_InputMapping.PlayerInput.Hotkey1.performed += _ => { OnHotkeyPerformed(1); };
        m_InputMapping.PlayerInput.Hotkey2.performed += _ => { OnHotkeyPerformed(2); };
        m_InputMapping.PlayerInput.Hotkey3.performed += _ => { OnHotkeyPerformed(3); };
        m_InputMapping.PlayerInput.Hotkey4.performed += _ => { OnHotkeyPerformed(4); };
        m_InputMapping.PlayerInput.Hotkey5.performed += _ => { OnHotkeyPerformed(5); };
        m_InputMapping.PlayerInput.Hotkey6.performed += _ => { OnHotkeyPerformed(6); };

        m_InputMapping.PlayerInput.QuickBar1.performed += _ => { OnQuickBarPerformed(1);};
        m_InputMapping.PlayerInput.QuickBar2.performed += _ => { OnQuickBarPerformed(2);}; 
        
        m_InputMapping.PlayerInput.Enable();
        LoadBindings(); // 启动时加载已保存的改键

        InputSystem.onActionChange += OnActionChange;
    }



    private void OnQuickBarPerformed(int index)
    {
        m_QuickBarEvt.Index = index;
        this.Publish(m_QuickBarEvt);
    }

    private void OnHotkeyPerformed(int index)
    {
        m_HotkeyEvt.Index = index;
        this.Publish(m_HotkeyEvt);
    }


    private bool _isKeyboardMouse;

    public bool IsKeyboardMouse()
    {
        return _isKeyboardMouse;
    }

    private void OnActionChange(object obj, InputActionChange change)
    {
        if (change == InputActionChange.ActionPerformed)
        {
            var action = obj as InputAction;
            if (action == null) return;

            var device = action.activeControl?.device;

            _isKeyboardMouse = IsKeyboardOrMouse(device);
        }
    }

    private bool IsKeyboardOrMouse(InputDevice device)
    {
        if (device == null) return false;

        // 方法1：推荐（最可靠）
        if (device is Keyboard || device is Mouse)
            return true;

        // 方法2：通过 description 判断（兼容性好）
        var deviceClass = device.description.deviceClass;
        return deviceClass == "Keyboard" || deviceClass == "Mouse";

        // 方法3：通过名称判断（备用）
        // return device.name.Contains("Keyboard") || device.name.Contains("Mouse");
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

    private void OnStatsPerformed(InputAction.CallbackContext _)
    {
        this.Publish(m_StatsEvt);
    }
    
    private void OnSkipPerformed(InputAction.CallbackContext obj)
    {
        this.Publish(_skipEvt);
    }

    private void OnEscPerformed(InputAction.CallbackContext obj)
    {
        this.Publish(m_EscPressedEvt);
    }

    private void OnSwitchPerformed(InputAction.CallbackContext obj)
    {
        this.Publish(m_SwitchEvt);
    }

    public void EnableInput()
    {
        m_InputMapping.PlayerInput.Enable();
    }

    /// <summary>
    ///     屏蔽整个输入系统（切换场景时推荐调用）
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