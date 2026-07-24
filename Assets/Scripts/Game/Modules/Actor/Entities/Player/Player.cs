using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

// wasd move
// left interact
// right menu
//[DefaultExecutionOrder(0)]
public partial class Player : Entity, IDynamicEntity
{
    [SerializeField] private Transform m_AvatarRoot;
    private TurnActor m_TurnActor;
    private AgentStats m_AgentStats;
    private AgentMover m_AgentMover;
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
        this.SubscribeInput<InputManager.OverworldEvt>(OnOverworldInputEvt);
        m_TurnActor = gameObject.AddComponent<TurnActor>();

        m_AgentStats = gameObject.AddComponent<AgentStats>();
        m_AgentMover = gameObject.AddComponent<AgentMover>();
        m_AgentAbilities = gameObject.AddComponent<AgentAbilities>();
        var handle = Addressables.LoadAssetAsync<Ability>("Ability/UnArmed");
        handle.Completed += operationHandle => { m_AgentAbilities.SetUnArmedAbility(operationHandle.Result); };

        m_AgentAnimations = gameObject.GetComponent<AgentAnimations>();
        m_AgentInteractive = gameObject.AddComponent<AgentInteractive>();
        Faction = EntityFaction.Player;

        m_AgentAnimations.Setup(m_AvatarRoot);
        _xyPlane = new Plane(Vector3.back, Vector3.zero);   // 法线朝 -Z，点在原点
        EntityManager.Register(this);
    }



    private Vector2 m_InputDirection = Vector2.zero;

    private UniTask OnDefeated(AgentStats.DefeatedEvent evt)
    {
        TurnManager.UnRegister(m_TurnActor);
        PathFinder.Instance.ClearLogical(this);
        
        Destroy(gameObject);

        // 玩家被击败时的处理（如结算、复活、UI 等）
        return UniTask.CompletedTask;
    }

    private async UniTask OnWASDEvt(InputManager.WASDEvt arg)
    {
        m_InputDirection = arg.Direction;
        _lootUnitTarget = null;
        await UniTask.CompletedTask;
    }

    private async UniTask OnTurnAction(TurnActor.TurnActionEvent arg)
    {
        RefreshCombatState(this);

        // 敌人可能在玩家上一动之后才进入 Engaged，当次 HandlePath 时 IsInCombatMode 仍为 false，路径未清；回合开始时若已在战斗则丢弃剩余多格路径，避免自动连走
        if (IsInCombatMode)
        {
            ClearPath();
            if (TileSelector.HasInstance())
                TileSelector.Instance.ClearPath();
        }

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
        /*m_InputDirection = Vector2.zero;
        if (InputSystem.HasInstance())
        {
            var movement = InputSystem.Instance.GetInputMapping().PlayerInput.Movement.ReadValue<Vector2>();
            if (_InputAvailable)
            {
                ClearPath();
                m_InputDirection = movement;
            }
        }*/

        if (_Controllable && keyboardInputEnabled && HandleKeyboardControls())
        {
        }

        DoNotClickUIAndGameAtSameTime();

        // var velocity = m_AgentMover.IsMoving() ? 1 : 0;
        // m_AgentAnimations.UpdateBaseAnimation(velocity);

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
    
    private async UniTask OnOverworldInputEvt(InputManager.OverworldEvt arg)
    {
        if (!UIRoot.HasInstance())
        {
            await UniTask.CompletedTask;
            return;
        }
        
        await UIRoot.Instance.Toggle(Const.KeyUI.Overworld);
    }

    protected override bool IsWalkable(PathCell cell, int goalX, int goalZ)
    {
        if (cell.Logical == null)
            return true;

        // 判断当前 cell 是否属于 Agent 在【终点】时会占据的矩形区域
        var isInGoalFootprint = PathFinder.IsCellInGoalFootprint(this, cell, goalX, goalZ);
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