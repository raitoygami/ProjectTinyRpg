using System;
using System.Runtime.CompilerServices;
using cfg;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

// ReSharper disable All

/// <summary>
/// 敌对/召唤 AI 实体：关卡敌人与技能召唤物共用；刷怪器创建时走 <see cref="ConfigureAsEnemy"/>，
/// 技能召唤走 <see cref="ConfigureAsSummon"/>（存在回合数耗尽后移除）。
/// </summary>
[DefaultExecutionOrder(1)]
public class AIEntity : Entity
{
    [SerializeField] private Transform m_AvatarRoot;
    [SerializeField] private Transform m_SpriteRoot;

    private Weapon _WeaponInstance;
    
    private AgentStats m_AgentStats;
    private TurnActor m_TurnActor;
    private AgentMover m_AgentMover;
    private AgentAbilities m_AgentAbilities;
    private AgentAnimations m_AgentAnimations;
    private AgentWeapon m_AgentWeapon;
    private Blackboard _blackBoard;
    private Vector3 _spawnPointLocation;
    private Vector3 _spawnLocation;
    private int m_DisengageLeashRange;

    private int _lifetimeTurnsRemaining;
    private Action m_UnsubscribeLifetime;

    private IAIStrategy _aiStrategy;

    /// <summary>召唤施法者（主人）；仅召唤流程设置，关卡敌等为 null。</summary>
    public Entity SummonOwner { get; private set; }

    public bool HasLeashConfigured => m_DisengageLeashRange > 0;
    public int DisengageLeashRange => m_DisengageLeashRange;
    public Vector3 SpawnLocation => _spawnLocation;
    public Vector3 SpawnPointLocation => _spawnPointLocation;
    public void SetHomeAnchor(Vector3 spawnLocation, Vector3 spawnPointLocation, int disengageLeashRange)
    {
        _spawnLocation = spawnLocation;
        _spawnPointLocation = spawnPointLocation;
        m_DisengageLeashRange = Mathf.Max(0, disengageLeashRange);
    }
    
    /// <summary>强制清空当前 AI 策略的运行时状态（不改变策略类型）。</summary>
    public void ResetAi()
    {
        _aiStrategy?.Reset();
    }

    protected void Awake()
    {
        GridSizeX = 1;
        GridSizeZ = 1;
  
        m_TurnActor = gameObject.AddComponent<TurnActor>();
        m_AgentMover = gameObject.AddComponent<AgentMover>();
        m_AgentStats = gameObject.AddComponent<AgentStats>();
        m_AgentAbilities = gameObject.AddComponent<AgentAbilities>();
        
        _blackBoard = new Blackboard(this);

        this.Subscribe<TurnActor.TurnActionEvent>(OnTurnAction);
        this.Subscribe<AgentMover.MoveStartEvent>(OnMoveStart);
        this.Subscribe<AgentMover.MoveFinishEvent>(OnMoveFinish);
        this.Subscribe<AgentMover.MoveForcedFinishEvent>(MoveForcedFinishEvent);
        this.Subscribe<AgentStats.HealthChangedEvent>(OnHealthChanged);
        this.Subscribe<AgentStats.DefeatedEvent>(OnDefeated);

        m_AgentAnimations = gameObject.AddComponent<AgentAnimations>();
        m_AgentAnimations.Setup(m_AvatarRoot, m_SpriteRoot);
        
        
    }

    private EnemyStatData _runtimeStat;
    private AsyncOperationHandle<GameObject> _weaponHandle;
    public async UniTask SetEntityState(EnemyStatData statData)
    {
        var entityTemplateTable = ConfigManager.Instance.ScriptableContainer.EntityTemplateTable;
        var template = entityTemplateTable.GetTemplate(statData.EntityId);
        if (template != null)
        {
            m_AgentWeapon = gameObject.GetOrAddComponent<AgentWeapon>();
            _weaponHandle = Addressables.LoadAssetAsync<GameObject>(template.DefaultWeapon);
            await _weaponHandle.ToUniTask();
            m_AgentWeapon.LoadEnemyWeapon(_weaponHandle.Result.GetComponent<Weapon>());
        }
        m_AgentStats.SetHealthLost(statData.HpLost);
        m_AgentAnimations.SetDirection(statData.Direction);
        // 获取技能数据
        await m_AgentAbilities.UpdateWepAbility(m_AgentWeapon.WeaponCurrent().WepAtkAbilityId);
        var abilities = statData.GetAbilities();
        await m_AgentAbilities.SyncWepAtkAbilityStat(abilities.LookupTable);
        // 初始化的时候_runtimeStat为null， 所以不会在写回到存档, 但是会通知ui界面UIStatBar更新血量信息
        await this.Publish(new AgentStats.HealthChangedEvent()
        {
            Stats = m_AgentStats,
            Current = m_AgentStats.HealthCurrent,
            Max = m_AgentStats.MaxHealth,
        });
         
        _runtimeStat = statData;
        m_AgentAnimations.RefreshVisibility();
        
    }
    
    protected override bool IsWalkable(PathCell cell, int goalX, int goalY)
    {
        if (cell.Logical == null)
            return true;
        
        /*var layer = (int) Mathf.Log(cell.Logical.Layer.value, 2);
        Debug.Log($"Enemy cell.Logical.Layer.value-{cell.Logical.Layer.value}-{LayerMask.LayerToName(layer)}");*/
        // 判断当前 cell 是否属于 Agent 在【终点】时会占据的矩形区域
        var isInGoalFootprint = PathFinder.IsCellInGoalFootprint(this, cell, goalX, goalY);

        if (isInGoalFootprint)
        {
            // 终点区域：只阻挡真正的 Obstacle, Interact
            return (Const.Layer.ObstacleForEnemyNavi.value & cell.Logical.Layer.value) == 0;
        }

        // 非终点区域：正常阻挡 Creature、Interact 等
        return (Const.Layer.ObstacleForNavi.value & cell.Logical.Layer.value) == 0;
    }
    
    /// <summary>由 <see cref="EntityManager.CreateEnemy"/> 在实例化后立即调用。</summary>
    public void ConfigureAsEnemy(t_Entity entity)
    {
        SummonOwner = null;
        Faction = EntityFaction.Enemy;
        ApplyEntityConfig(entity);
        EntityManager.Register(this);
    }

    public void ConfigureAsSummon(EntityFaction summonFaction, t_Entity entityConfig, int lifetimeTurns,
        Entity summonOwner)
    {
        Faction = summonFaction;
        SummonOwner = summonOwner;
        ApplyEntityConfig(entityConfig);
        EntityManager.Register(this);
        _lifetimeTurnsRemaining = lifetimeTurns;
        m_UnsubscribeLifetime = this.Subscribe<TurnActor.TurnStartedEvent>(OnSummonTurnStarted);
    }

    private void ApplyEntityConfig(t_Entity entity)
    {
        if (m_AgentStats != null && entity?.Attr != null)
            m_AgentStats.SetBaseFromAttribute(entity.Attr);
        m_AgentStats?.SetEntityConfig(entity);
        if (!string.IsNullOrEmpty(entity?.Addressable))
            GetComponent<AgentAvatar>().SetDisplayFromAddressable(entity.Addressable);
        RebuildAiStrategy(entity);
    }

    private void RebuildAiStrategy(t_Entity entity)
    {
        AIParameterTable.AIParameterData parameterData = null;
        
        if (ConfigManager.HasInstance())
            parameterData = ConfigManager.Instance.ScriptableContainer.AIParameterTable.GetData(entity.Id);
        var pattern = parameterData?.Pattern ?? AIPattern.Default;
        _aiStrategy = AiStrategyFactory.Create(pattern);
        _aiStrategy.Initialize(this, _blackBoard);
    }

    public override void OnUpdate()
    {
        var velocity = m_AgentMover.IsMoving() ? 1 : 0;
        m_AgentAnimations.UpdateBaseAnimation(velocity);
    }

    private async UniTask OnSummonTurnStarted(TurnActor.TurnStartedEvent arg)
    {
        if (m_UnsubscribeLifetime == null) return;

        if (_lifetimeTurnsRemaining <= 0)
        {
            arg.AbortTurn = true;
            ExpireFromLifetime();
            return;
        }

        _lifetimeTurnsRemaining--;
        await UniTask.CompletedTask;
    }

    private void ExpireFromLifetime()
    {
        m_UnsubscribeLifetime?.Invoke();
        m_UnsubscribeLifetime = null;
        TurnManager.UnRegister(m_TurnActor);
        PathFinder.Instance.ClearLogical(this);
        KillLocalTweensBeforeDestroy();
        Destroy(gameObject);
    }

    /// <summary>
    /// 在 Destroy 之前杀掉子物体/根 Transform 上的补间；避免子物体先销毁后 OnDisable 里无法 DOKill 导致 DOTween 仍引用已毁 Transform。
    /// </summary>
    private void KillLocalTweensBeforeDestroy()
    {
        m_AgentAnimations?.KillAllTween();
        if (m_AvatarRoot != null)
            m_AvatarRoot.DOKill(false);
        transform.DOKill(false);
    }

    private AiContext _aiContext;
    private async UniTask OnTurnAction(TurnActor.TurnActionEvent arg)
    {
        AIParameterTable.AIParameterData parameterData = null;
        if (ConfigManager.HasInstance())
        {
            var ec = m_AgentStats.EntityConfig;
            parameterData = ConfigManager.Instance.ScriptableContainer.AIParameterTable.GetData(ec.Id);
        }

        if (parameterData == null)
            return;
        
        if (_aiStrategy == null)
            RebuildAiStrategy(m_AgentStats != null ? m_AgentStats.EntityConfig : null);
        
        
        _aiContext ??= new AiContext
        {
            Owner = this,
            Board = _blackBoard,
            Parameter = parameterData.Parameter
        };
        await _aiStrategy.ExecuteTurn(_aiContext);
        
        m_TurnActor.FinishTurn();
    }

    private UniTask OnMoveStart(AgentMover.MoveStartEvent arg)
    {
        if (!arg.Forced)
            m_AgentAnimations.FaceTarget(arg.TargetPosition - arg.StartPosition);


        return UniTask.CompletedTask;
    }

    private UniTask MoveForcedFinishEvent(AgentMover.MoveForcedFinishEvent arg)
    {
        if (_runtimeStat != null)
        {
            _runtimeStat.Location = arg.CurrPosition;
            _runtimeStat.Direction = m_AgentAnimations.GetDirection();
        }
        
        if (FOVManager.HasInstance())
            FOVManager.Instance.RefreshVisibility(this, Vector3Int.FloorToInt(arg.CurrPosition));
        
        // 被迫移动结束后要重新计算技能预览
        _blackBoard?.RefreshTelegraph();
        return UniTask.CompletedTask;
    }
    
    private UniTask OnMoveFinish(AgentMover.MoveFinishEvent arg)
    {
        if (_runtimeStat != null)
        {
            _runtimeStat.Location = arg.CurrPosition;
            _runtimeStat.Direction = m_AgentAnimations.GetDirection();
        }
        
        if (FOVManager.HasInstance())
            FOVManager.Instance.RefreshVisibility(this, Vector3Int.FloorToInt(arg.CurrPosition));

        
        return UniTask.CompletedTask;
    }
    
    private UniTask OnHealthChanged(AgentStats.HealthChangedEvent arg)
    {
        if (_runtimeStat != null)
        {
            _runtimeStat.HpLost = arg.HpLost;
        }
        return UniTask.CompletedTask;
    }
    
    private async UniTask OnDefeated(AgentStats.DefeatedEvent evt)
    {
        if (_runtimeStat != null)
            _runtimeStat.IsAlive = false;
        
        m_UnsubscribeLifetime?.Invoke();
        m_UnsubscribeLifetime = null;
        TurnManager.UnRegister(m_TurnActor);
        PathFinder.Instance.ClearLogical(this);
        CombatManager.Instance.RemoveEnemyTarget(this);
        KillLocalTweensBeforeDestroy();
        await m_AgentAnimations.Death();
        Destroy(gameObject);
        
        await UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        Addressables.Release(_weaponHandle);
        EntityManager.UnRegister(this);
        _blackBoard?.Clear();
        _blackBoard = null;
        transform.DOKill();
    }
}
