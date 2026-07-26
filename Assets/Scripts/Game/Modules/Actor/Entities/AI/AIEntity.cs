using System;
using cfg;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
    [SerializeField] private Weapon m_Weapon;

    private Weapon _WeaponInstance;
    
    private AgentStats m_AgentStats;
    private TurnActor m_TurnActor;
    private AgentMover m_AgentMover;
    private AgentAbilities m_AgentAbilities;
    private AgentAnimations m_AgentAnimations;
    private AgentWeapon m_AgentWeapon;
    private Blackboard m_BlackBoard;

    private Vector3 m_HomeGridPosition;
    private int m_DisengageLeashRange;

    private int _lifetimeTurnsRemaining;
    private Action m_UnsubscribeLifetime;

    private IAiStrategy _aiStrategy;

    /// <summary>召唤施法者（主人）；仅召唤流程设置，关卡敌等为 null。</summary>
    public Entity SummonOwner { get; private set; }

    public bool HasLeashConfigured => m_DisengageLeashRange > 0;

    public Vector3 HomeGridPosition => m_HomeGridPosition;

    public int DisengageLeashRange => m_DisengageLeashRange;

    public void SetHomeAnchor(Vector3 spawnGridPosition, int disengageLeashRange)
    {
        m_HomeGridPosition = spawnGridPosition;
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
        
        m_BlackBoard = new Blackboard(this);

        this.Subscribe<TurnActor.TurnActionEvent>(OnTurnAction);
        this.Subscribe<AgentMover.MoveStartEvent>(OnMoveStart);
        this.Subscribe<AgentStats.DefeatedEvent>(OnDefeated);

        m_AgentAnimations = gameObject.AddComponent<AgentAnimations>();
        m_AgentAnimations.Setup(m_AvatarRoot, m_SpriteRoot);
        
        m_AgentWeapon = gameObject.GetComponent<AgentWeapon>();
        m_AgentWeapon.LoadWeapon(m_Weapon);
        m_AgentAbilities.UpdateWepAbility(m_AgentWeapon.WeaponCurrent().AbilityNormalAtk);
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
        t_AI aiCfg = null;
        if (entity?.AiId != null && ConfigManager.HasInstance())
            aiCfg = ConfigManager.Instance.Tables?.DataAI?.GetOrDefault(entity.AiId.Value);
        var pattern = aiCfg?.AiPattern ?? AiPattern.Default;
        _aiStrategy = AiStrategyRegistry.Create(pattern);
        _aiStrategy.Initialize(this, m_BlackBoard);
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
        m_AgentAnimations?.KillAllTweens();
        if (m_AvatarRoot != null)
            m_AvatarRoot.DOKill(false);
        transform.DOKill(false);
    }

    private async UniTask OnTurnAction(TurnActor.TurnActionEvent arg)
    {
        t_AI aiCfg = null;
        if (ConfigManager.HasInstance())
        {
            var ec = m_AgentStats.EntityConfig;
            if (ec?.AiId != null)
                aiCfg = ConfigManager.Instance.Tables?.DataAI?.GetOrDefault(ec.AiId.Value);
        }

        if (_aiStrategy == null)
            RebuildAiStrategy(m_AgentStats != null ? m_AgentStats.EntityConfig : null);

        await _aiStrategy.ExecuteTurn(new AiContext
        {
            Owner = this,
            Board = m_BlackBoard,
            AiConfig = aiCfg
        });
        
        m_TurnActor.FinishTurn();
    }

    private UniTask OnMoveStart(AgentMover.MoveStartEvent arg)
    {
        if (!arg.Forced)
            m_AgentAnimations.FaceTarget(arg.TargetPosition - arg.StartPosition);

        return UniTask.CompletedTask;
    }

    private UniTask OnDefeated(AgentStats.DefeatedEvent evt)
    {
        m_UnsubscribeLifetime?.Invoke();
        m_UnsubscribeLifetime = null;
        TurnManager.UnRegister(m_TurnActor);
        PathFinder.Instance.ClearLogical(this);
        KillLocalTweensBeforeDestroy();
        Destroy(gameObject);
        return UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        EntityManager.UnRegister(this);
        /*if (Context.HasInstance())
            Player.RefreshCombatState(Context.Instance.PlayerInst);*/
    }
}
