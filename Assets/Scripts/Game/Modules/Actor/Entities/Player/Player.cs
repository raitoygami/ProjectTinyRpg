using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

// wasd move
// left interact
// right menu
//[DefaultExecutionOrder(0)]
public partial class Player : Entity
{
    [SerializeField] private Transform m_AvatarRoot;
    [SerializeField] private Transform m_SpriteRoot;
    private TurnActor m_TurnActor;
    private AgentStats m_AgentStats;
    private AgentMover m_AgentMover;
    private AgentWeapon m_AgentWeapon;
    private AgentAbilities m_AgentAbilities;
    private AgentAnimations m_AgentAnimations;
    private AgentInteractive m_AgentInteractive;

    private Vector2 pointerInput, movementInput;

    // internal state
    private bool _InputAvailable;
    private bool _Controllable;
    private GameObject _CoverTarget;
    private bool onNextTurnSkipPlayerActions;
    private readonly Queue<Func<UniTask>> m_NextTurnEvt = new();
    protected void Awake()
    {
        GridSizeX = 1;
        GridSizeZ = 1;
   
        this.Subscribe<TurnActor.TurnActionEvent>(OnTurnAction);
        this.Subscribe<TurnActor.TurnEndedEvent>(OnTurnEnded);

        this.Subscribe<AgentMover.MoveStartEvent>(MoveStartEvent);
        this.Subscribe<AgentMover.MoveFinishEvent>(MoveFinishEvent);
        this.Subscribe<AgentStats.DefeatedEvent>(OnDefeated);

        this.SubscribeInput<InputManager.PointerMoveEvt>(OnPointerMoveEvt);
        this.SubscribeInput<InputManager.MouseClickEvt>(OnMouseClickEvt);
        this.SubscribeInput<InputManager.WASDEvt>(OnWASDEvt);
        this.SubscribeInput<InputManager.InventoryEvt>(OnInventoryInputEvt);
        this.SubscribeInput<InputManager.SkipEvt>(OnSkipTurn);

        this.SubscribeGlobal<EnterCombatEvt>(OnEnterCombatEvt);
        
        m_TurnActor = gameObject.AddComponent<TurnActor>();

        m_AgentStats = gameObject.AddComponent<AgentStats>();
        m_AgentMover = gameObject.AddComponent<AgentMover>();
        m_AgentAbilities = gameObject.AddComponent<AgentAbilities>();

        m_AgentAnimations = gameObject.GetComponent<AgentAnimations>();
        m_AgentInteractive = gameObject.AddComponent<AgentInteractive>();
        // 加载武器
        this.Subscribe<AgentWeapon.WeaponChangeEvt>(OnWeaponChanged);
        m_AgentWeapon = GetComponent<AgentWeapon>();
        m_AgentWeapon.LoadWeapon("Weapon/Wep_Hammer_01").Forget();
        
        Faction = EntityFaction.Player;

        m_AgentAnimations.Setup(m_AvatarRoot, m_SpriteRoot);
        _xyPlane = new Plane(Vector3.back, Vector3.zero);   // 法线朝 -Z，点在原点
        EntityManager.Register(this);
    }

    private UniTask OnWeaponChanged(AgentWeapon.WeaponChangeEvt arg)
    {
        m_AgentAbilities.UpdateWepAbility(arg.WepNormalAtk);
        return UniTask.CompletedTask;
    }


    private Vector2 m_InputDirection = Vector2.zero;

    private UniTask OnDefeated(AgentStats.DefeatedEvent evt)
    {
        TurnManager.UnRegister(m_TurnActor);
        TurnManager.Instance.StopLoop();
        TileSelector.Instance.ClearPath();
        PathFinder.Instance.ClearLogical(this);
        
        Destroy(gameObject);

        // 玩家被击败时的处理（如结算、复活、UI 等）
        return UniTask.CompletedTask;
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

        if (m_NextTurnEvt.Count > 0)
        {
            while (m_NextTurnEvt.Count > 0)
            {
                var callback = m_NextTurnEvt.Dequeue();
                await callback();
            }

            if (onNextTurnSkipPlayerActions)
            {
                ClearPath();
                GetComponent<TurnActor>().FinishTurn();
                return;
            }
        }

        _ = _Path is { Count: > 0 } ? HandlePath() : OnPointerMoveEvt(null);

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
        m_NextTurnEvt.Enqueue(callback);
        if (!skipPlayerAction) return;

        if (!onNextTurnSkipPlayerActions)
        {
            onNextTurnSkipPlayerActions = true;
        }
    }


    private void CoverTarget(GameObject target, bool cover)
    {
        if (target == null)
            return;

        _CoverTarget = cover ? target : null;

        if (target.GetComponentInParent<AgentAvatar>() != null)
        {
            target.GetComponentInParent<AgentAvatar>().Cover(cover);
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

        if (_Controllable && keyboardInputEnabled && HandleKeyboardControls())
        {
        }

        DoNotClickUIAndGameAtSameTime();

        var velocity = m_AgentMover.IsMoving() ? 1 : 0;
        m_AgentAnimations.UpdateBaseAnimation(velocity);

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
        Debug.Log("OnInventoryInputEvt");
        await UIRoot.Instance.Toggle(Const.KeyUI.Inventory);
    }
    
    private async UniTask OnSkipTurn(InputManager.SkipEvt arg)
    {
        if (_PreparingAbility)
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
        EntityManager.UnRegister(this);
    }

}