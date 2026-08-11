using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.AddressableAssets;

// wasd move
// left interact
// right menu
//[DefaultExecutionOrder(0)]
public partial class Player : Entity
{
    [SerializeField] private Transform m_AvatarRoot;
    [SerializeField] private Transform m_SpriteRoot;
    private TurnActor _turnActor;
    private AgentStats _agentStats;
    private AgentAvatar _agentAvatar;
    private AgentMover _agentMover;
    private AgentWeapon _agentWeapon;
    private AgentAbilities _agentAbilities;
    private AgentAnimations _agentAnimations;
    private AgentInteractive _agentInteractive;
    private AgentCustomization _agentCustomization;
    
    private AgentWeapon.EquipChangedEvt _equipChangedEvt;
    
    // internal state
    private bool _InputAvailable;
    private bool _Controllable;
    private IInteractable _interactableHovered;
    private bool onNextTurnSkipPlayerActions;
    private readonly Queue<Func<UniTask>> _nextTurnEvt = new();
    protected void Awake()
    {
        GridSizeX = 1;
        GridSizeZ = 1;
   
        this.Subscribe<TurnActor.TurnActionEvent>(OnTurnAction);
        this.Subscribe<TurnActor.TurnEndedEvent>(OnTurnEnded);

        this.Subscribe<AgentMover.MoveStartEvent>(MoveStartEvent);
        this.Subscribe<AgentMover.MoveFinishEvent>(MoveFinishEvent);
        this.Subscribe<AgentMover.MoveForcedFinishEvent>(MoveForcedFinishEvent);
        this.Subscribe<AgentStats.HealthChangedEvent>(HealthChangedEvent);
        this.Subscribe<AgentStats.DefeatedEvent>(OnDefeated);

        // 切换武器
        this.Subscribe<AgentWeapon.EquippedWeaponChangeEvt>(OnWeaponChanged);
        
        this.SubscribeInput<InputManager.PointerMoveEvt>(OnPointerMoveEvt);
        this.SubscribeInput<InputManager.MouseClickEvt>(OnMouseClickEvt);
        this.SubscribeInput<InputManager.WASDEvt>(OnWASDEvt);
        this.SubscribeInput<InputManager.InventoryEvt>(OnInventoryInputEvt);
        this.SubscribeInput<InputManager.StatsEvt>(OnStatsEvt);
        this.SubscribeInput<InputManager.SkipEvt>(OnSkipTurn);
        this.SubscribeInput<InputManager.SwitchEvt>(OnSwitchWeapon);
        this.SubscribeInput<InputManager.HotkeyEvt>(OnHotKey);
        this.SubscribeInput<InputManager.QuickBarEvt>(OnQuickBar);
        
        // 装备变动
        this.SubscribeGlobal<Context.EquipmentUpdateEvt>(OnItemChanged);
        this.SubscribeGlobal<Context.FOVDirtyEvt>(OnFovDirtyEvt);
  
        _turnActor = gameObject.AddComponent<TurnActor>();
        
        _agentStats = gameObject.AddComponent<AgentStats>();
        _agentMover = gameObject.AddComponent<AgentMover>();
        _agentAvatar = gameObject.GetComponent<AgentAvatar>();
        _agentWeapon = gameObject.GetComponent<AgentWeapon>();
        _agentAbilities = gameObject.AddComponent<AgentAbilities>();
        _agentAnimations = gameObject.GetComponent<AgentAnimations>();
        _agentInteractive = gameObject.AddComponent<AgentInteractive>();
        _agentCustomization = gameObject.GetComponent<AgentCustomization>();
  
        _equipChangedEvt = new AgentWeapon.EquipChangedEvt();
        // 订阅装备增删改
        PlayerManager.Instance.OnEquipmentChanged += OnEquipmentChanged;
        
        Faction = EntityFaction.Player;

        _agentAnimations.Setup(m_AvatarRoot, m_SpriteRoot);
        _xyPlane = new Plane(Vector3.back, Vector3.zero);   // 法线朝 -Z，点在原点
        EntityManager.Register(this);
    }

 

    private UniTask HealthChangedEvent(AgentStats.HealthChangedEvent arg)
    {
        var playerStats = PlayerManager.Instance.GetStats();
        if (playerStats != null)
        {
            playerStats.HpLost = arg.HpLost;    
        }
        return UniTask.CompletedTask;
    }

    public Transform GetAvatarRoot()
    {
        return m_AvatarRoot;
    }
    
    private void OnEquipmentChanged(int location, ItemStack itemStackOld, ItemStack itemStackNew)
    {
        
        if (itemStackOld != null)
        {
            _agentStats.RemoveAttributeModifiersFromSource(itemStackOld);
        }

        if (itemStackNew != null)
        {
            _agentStats.AddAttributeModifiersFromSource(itemStackNew.GetModifiers(), itemStackNew);
        }

        this.PublishGlobal(Context.PlayerStatsChange);
        // m_AgentStats.LogPlayerAttributesDebug();
    }
    
    private UniTask OnQuickBar(InputManager.QuickBarEvt arg)
    {
        Debug.Log($"QuickBar {arg.Index}");
        return UniTask.CompletedTask;
    }

    private UniTask OnHotKey(InputManager.HotkeyEvt arg)
    {
        Debug.Log($"HotKey {arg.Index}");
        return UniTask.CompletedTask;
    }

    private async UniTask OnSwitchWeapon(InputManager.SwitchEvt arg)
    {
        var location = PlayerManager.Instance.GetNextWeaponLocation();
        if (location == -1)
            return;

        var nextWeaponUID = PlayerManager.Instance.GetWeaponUID(location);
        
        if (PlayerManager.Instance.SetCurrWeaponUID(nextWeaponUID))
        {
            await this.PublishGlobal(Context.EquipmentUpdate);
        }
        
    }
    
    // 当道具发生变动的时候
    private async UniTask OnItemChanged(Context.EquipmentUpdateEvt arg)
    {
        await RefreshWeapons();
        // 更新换装
        _agentCustomization.RefreshCustomization();
        _agentAvatar.SetSprite(_agentCustomization.GetCombinedSprite());
        // 更新ui界面
        await this.PublishGlobal(Context.AvatarChanged);
    }

    private UniTask OnFovDirtyEvt(Context.FOVDirtyEvt arg)
    {
        // 更新fov
        FOVManager.Instance.FovCompute(GridPosition, FOVManager.PlayerViewDistance);
        FOVManager.Instance.PlayerVisibilityChanged();
        return UniTask.CompletedTask;
    }
    
    // 第一次实例化
    public async UniTask FirstBindAfterInst()
    {
        var entityID = PlayerManager.Instance.GetEntityID();
        var entityTemplateTable = ConfigManager.Instance.ScriptableContainer.EntityTemplateTable;
        var template = entityTemplateTable.GetTemplate(entityID);
        if (template != null)
        {
            var handle = Addressables.LoadAssetAsync<GameObject>(template.DefaultWeapon);
            await handle.ToUniTask();
            _agentWeapon.LoadUnarmedWeapon(handle.Result.GetComponent<Weapon>());
        }
        
        // 更新装备外观
        _agentCustomization.RefreshCustomization();
        _agentAvatar.SetSprite(_agentCustomization.GetCombinedSprite());

        // 更新武器技能和外观
        await RefreshWeapons();
        
        // 更新属性 
        AddEquipmentModifiers();
        
        // 读取当前血量
        var playerStats =  PlayerManager.Instance.GetStats();
        _agentStats.SetHealthLost(playerStats.HpLost);

        var playerLocation = PlayerManager.GetLocation();
        _agentAnimations.SetDirection(playerLocation.Direction);

        // 更新fov
        FOVManager.Instance.FovCompute(GridPosition, FOVManager.PlayerViewDistance);
        
    }

    private async UniTask RefreshWeapons()
    {
        // 这里不会移除已经缓存的武器实例
        // 但是会移除没有装备的武器普通攻击技能
        var wepAtkAbilityIDs = new List<int>();
        for (var i = 4; i < 8; i++)
        {
            var weaponUid = PlayerManager.Instance.GetWeaponUID(i);
            var weapon = await _agentWeapon.InitWeapon(weaponUid);
            if (weapon != null)
                wepAtkAbilityIDs.Add(weapon.WepAtkAbilityId);
        }
        await _agentAbilities.SyncWepAbilities(wepAtkAbilityIDs);
        await _agentAbilities.SyncWepAtkAbilityStat(PlayerManager.Instance.GetAbilities().LookupTable);
        
        // 直接通过存档数据加载武器
        var currWeaponUIDEquipped = PlayerManager.Instance.GetCurrWeaponUID(); // 默认-1
        await _agentWeapon.SwapWeapon(currWeaponUIDEquipped);
    }

    // 这个函数，只在初始化的时候调用一次
    private void AddEquipmentModifiers()
    {
        var equippedItems = PlayerManager.Instance.GetSavedItemContainer().Equipped;
        foreach (var uid in equippedItems)
        {
            var itemStack = PlayerManager.Instance.GetItemStackByUID(uid);
            _agentStats.AddAttributeModifiersFromSource(itemStack.GetModifiers(), itemStack);
        }

        // m_AgentStats.LogPlayerAttributesDebug();
    }
    
    //  切换武器的时候, 同步一下 技能信息
    private async UniTask OnWeaponChanged(AgentWeapon.EquippedWeaponChangeEvt arg)
    {
        await _agentAbilities.UpdateWepAbility(arg.WepAtkAbilityID);
        
        // 只要切换武器就取消技能
        if (_isPreparingAbility && _abilityPrepared.GetAbilityID() != arg.WepAtkAbilityID)
            _abilityPrepared.Cancel();
        
        var abilityStat = PlayerManager.Instance.GetAbilityStat(arg.WepAtkAbilityID);
        await _agentAbilities.SyncWepAtkAbilityStat(arg.WepAtkAbilityID, abilityStat);
        // 全局消息 更新界面
        await this.PublishGlobal(arg);
    }

    private Vector2 m_InputDirection = Vector2.zero;

    private async UniTask OnDefeated(AgentStats.DefeatedEvent evt)
    {
        TurnManager.UnRegister(_turnActor);
        TurnManager.Instance.StopLoop();
        GridIndicatorManager.Instance.HideCursorMark();
        PathFinder.Instance.ClearLogical(this);
        await _agentAnimations.Death();
        Destroy(gameObject);
        // 玩家被击败时的处理（如结算、复活、UI 等）
        await UniTask.CompletedTask;
    }

    private async UniTask OnWASDEvt(InputManager.WASDEvt arg)
    {
        if (m_InputDirection == Vector2.zero)
        {
            await UniTask.DelayFrame(5);
            _InputAvailable = true;
        }
        else
        if (arg.Direction == Vector2.zero)
            _InputAvailable = false;
        await UniTask.CompletedTask;
    }

    private async UniTask OnTurnAction(TurnActor.TurnActionEvent arg)
    {
        ResetInput();

        if (CombatManager.HasInstance() && CombatManager.Instance.IsInBattle)
        {
            ClearPath();
            if (GridIndicatorManager.HasInstance())
                GridIndicatorManager.Instance.HideCursorMark();
        }
        
        if (_nextTurnEvt.Count > 0)
        {
            while (_nextTurnEvt.Count > 0)
            {
                var callback = _nextTurnEvt.Dequeue();
                await callback();
            }

            if (onNextTurnSkipPlayerActions)
            {
                ClearPath();
                GetComponent<TurnActor>().FinishTurn();
                return;
            }
        }

        _ = _pathNodes is { Count: > 0 } ? HandlePath() : OnPointerMoveEvt(null);

        await UniTask.CompletedTask;
    }

    private async UniTask OnTurnEnded(TurnActor.TurnEndedEvent arg)
    {
        _Controllable = false;
        onNextTurnSkipPlayerActions = false;
        await UniTask.CompletedTask;
    }

    public void OnNextTurn(Func<UniTask> callback, bool skipPlayerAction = true)
    {
        _nextTurnEvt.Enqueue(callback);
        if (!skipPlayerAction) return;

        if (!onNextTurnSkipPlayerActions)
        {
            onNextTurnSkipPlayerActions = true;
        }
    }

    private void OnEnable()
    {
        _ = OnPointerMoveEvt(null);
    }

    private void Update()
    {
        m_InputDirection = Vector2.zero;
        if (InputManager.HasInstance())
        {
            var movement = InputManager.Instance.GetInputMapping().PlayerInput.Movement.ReadValue<Vector2>();
            if (_InputAvailable)
            {
                ClearPath();
                m_InputDirection = movement;
            }
        }

        if (_Controllable && _keyboardInputEnabled && HandleKeyboardControls())
        {
        }

        DoNotClickUIAndGameAtSameTime();

        var velocity = _agentMover.IsMoving() ? 1 : 0;
        _agentAnimations.UpdateBaseAnimation(velocity);

        /*if (Input.GetKeyDown(KeyCode.P))
        {
            PersistenceModule.Instance.Save(0);
        }*/
    }

    private async UniTask OnInventoryInputEvt(InputManager.InventoryEvt _)
    {
        if (!UIRoot.HasInstance())
        {
            await UniTask.CompletedTask;
            return;
        }
        await UIRoot.Instance.Toggle(Const.KeyUI.Inventory);
    }
    
    private async UniTask OnStatsEvt(InputManager.StatsEvt _)
    {
        if (!UIRoot.HasInstance())
        {
            await UniTask.CompletedTask;
            return;
        }
        await UIRoot.Instance.Toggle(Const.KeyUI.Stats);
    }

    
    private async UniTask OnSkipTurn(InputManager.SkipEvt arg)
    {
        if (_isPreparingAbility)
            return;
        
        GetComponent<TurnActor>().FinishTurn(true);
        
        await UniTask.CompletedTask;
    }

    protected override bool IsWalkable(PathCell cell, int goalX, int goalY)
    {
        if (cell.Logical == null)
            return true;

        // 判断当前 cell 是否属于 Agent 在【终点】时会占据的矩形区域
        var isInGoalFootprint = PathFinder.IsCellInGoalFootprint(this, cell, goalX, goalY);
        /*Debug.Log($"{GetComponent<IPathNode>().GridSizeX}:{GetComponent<IPathNode>().GridSizeZ}");
        Debug.Log($"goalX:{goalX}-goalZ:{goalZ} Cell {cell.X}:{cell.Z}  {isInGoalFootprint}");*/
        if (isInGoalFootprint)
        {
            // 终点区域：只阻挡真正的 Obstacle，允许 Creature 和 Interact（用于攻击/交互）
            return (Const.Layer.ObstacleOnly.value & cell.Logical.Layer.value) == 0;
        }

        // 非终点区域：正常阻挡 Creature、Interact 等
        return (Const.Layer.ObstacleForNavi.value & cell.Logical.Layer.value) == 0;
    }
    
    private void OnDestroy()
    {
        if (PlayerManager.HasInstance())
        {
            PlayerManager.Instance.OnEquipmentChanged -= OnEquipmentChanged;
        }

        transform.DOKill();
        EntityManager.UnRegister(this);
    }

}