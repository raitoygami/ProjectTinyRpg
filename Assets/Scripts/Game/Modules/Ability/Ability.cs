using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Ability/Ability", fileName = "New Ability")]
public partial class Ability : ScriptableObject
{

    [SerializeField] private int _abilityID;
    
    [SerializeField] public LocalizedString AbilityName;
    [SerializeField] public Sprite Icon;

    public WeaponType WeaponTypeRequire;

    // 技能选取目标类型
    [SerializeField] private AbilityTargetType _targetType;
    
    // 技能施法范围
    [SerializeField] private int _castRange;
    // 技能施法类型
    [SerializeField] private CastTargetingMode _castTargetingMode;
    // 技能中心类型
    [SerializeField] private CastCenterType  _castCenterType;
    // 技能范围类型
    [SerializeField] private AffectType  _affectType;
    // 技能范围
    [Min(0)]
    [SerializeField] private int _range;
    
    // 是否需要准备动作
    [Min(0)]
    [SerializeField] private int _prepareTurn;


    [Tooltip("仅用于地面技能范围预览（与施法范围 m_Range 独立）；未配置则不绘制")] [SerializeReference] [SerializeField]
    private AbilityAffectRangeParam _abilityAffectRangeParam;

    
    [Min(1)]
    [SerializeField] private int _cooldown;

    //  cost
    [SerializeField] private int CostHP;
    [SerializeField] private int CostMP;

    public AbilityEffect TreeRoot;
    [SerializeField] public List<AbilityEffect> Effects = new();

    // internal state
    private enum State
    {
        Inactive,
        Selection,
        Execution
    }

    private State _state = State.Inactive;
    private Entity _owner;

    private AbilityStat _abilityStat;
    
    private UniTaskCompletionSource<bool> _getCastableRangeTask;

    public int GetAbilityID()
    {
        return _abilityID;
    }
    
    public void SetAbilityStat(AbilityStat abilityStat)
    {
        _abilityStat = abilityStat;
    }
    
    public int CoolDownRemaining()
    {
        return Mathf.Abs(_abilityStat.Cooldown - 1);
    }
    
    public bool IsOnCooldown()
    {
        return _abilityStat.Cooldown > 0;
    }

    public int GetPrepareTurn()
    {
        return _prepareTurn;
    }

    public int GetRange()
    {
        return _range;
    }
    
    public int GetCastRange()
    {
        return _castRange;
    }

    public CastTargetingMode GetCastTargetingMode()
    {
        return _castTargetingMode;
    }
    public CastCenterType GetCastCenterType()
    {
        return _castCenterType;
    }
    public AffectType GetAffectType()
    {
        return _affectType;
    }
    
    /// <summary>技能范围显示用参数（扇形/矩形/圆等），仅地面高亮；目标判定在子 Effect（如 <c>E_AOE</c>）中完成。</summary>
    public AbilityAffectRangeParam GetAbilityAffectRangeParam()
    {
        return _abilityAffectRangeParam;
    }

    public AbilityTargetType TargetMode()
    {
        return _targetType;
    }

    public bool IsTargeted()
    {
        return _targetType != AbilityTargetType.None && _targetType != AbilityTargetType.EmptyGround;
    }

    public void SetOwner(Entity owner)
    {
        if (owner == _owner) return;
        _owner.Unsubscribe<TurnActor.TurnEndedEvent>(OnTurnFinish);
        _owner = owner;
        _owner.Subscribe<TurnActor.TurnEndedEvent>(OnTurnFinish);
    }

    private UniTask OnTurnFinish(TurnActor.TurnEndedEvent args)
    {
        if (_abilityStat.Cooldown > 0)
        {
            _abilityStat.Cooldown--;
            _abilityStat.OnCooldownChanged?.Invoke();
        }

        return UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        _owner.Unsubscribe<TurnActor.TurnEndedEvent>(OnTurnFinish);
    }

    private List<Vector3Int> _castableRange;

    public List<Vector3Int> GetCastableRange(Vector3 castPoint, bool force = false)
    {
        if (_castableRange == null || force)
        {
            _castableRange =
                AbilityUtil.GetCastableRange( this, _owner);
        }
        return _castableRange;
    }
    
    public bool IsGridInCastRange(Vector3 gridPosition)
    {
        if (_owner == null)
            return false;
        var p = new Vector3Int((int)gridPosition.x, (int)gridPosition.y, 0);
        return GetCastableRange(gridPosition).Contains(p);
    }
    
    public bool TryGetSkillPreviewFrame(Vector3 ownerLocation, Vector3 castPoint, out Vector3 previewOriginGrid,
        out Vector3 skillFaceDirection)
    {
        previewOriginGrid = default;
        skillFaceDirection = default;
        if (_owner == null)
            return false;

        var owner = ownerLocation.Round();
        var mouse = castPoint.Round();

        skillFaceDirection = WorldExtensions.GridDelta(owner, mouse);
        if (skillFaceDirection.sqrMagnitude < 1e-8f)
            skillFaceDirection = Vector3.up;

        var line = owner.Line(mouse);
        Vector3? lastInRange = null;
        foreach (var step in line)
        {
            if (IsGridInCastRange(step))
                lastInRange = step;
        }

        if (lastInRange == null)
            return false;

        previewOriginGrid = lastInRange.Value;
        return true;
    }

    public UniTask<bool> PrepareCast(bool showRange = false)
    {
        _getCastableRangeTask = new UniTaskCompletionSource<bool>();
        if (_state != State.Inactive)
        {
            _getCastableRangeTask.TrySetResult(result: false);
            return _getCastableRangeTask.Task;
        }

        if (_abilityStat.Cooldown <= 0)
        {
            _state = State.Selection;
            _castableRange = AbilityUtil.GetCastableRange(this, _owner);
            if (showRange && GridIndicatorManager.HasInstance())
                GridIndicatorManager.Instance.ShowCastableRange(_castableRange);
            return _getCastableRangeTask.Task;
        }

        _getCastableRangeTask.TrySetResult(result: false);
        return _getCastableRangeTask.Task;
    }

    public async UniTask<bool> ExecuteMiss(Vector3 position, Entity target)
    {
        _castableRange = null;
        _state = State.Execution;
        GridIndicatorManager.Instance.HideAbilityPreview();

        var canceledByEffect = false;

        void OnContextCancel()
        {
            canceledByEffect = true;
        }
        
        var context = new AbilityContext
        {
            Owner = _owner,
            Target = target,
            Ability = this,
            Position = position,
            Cancel = OnContextCancel,
        };

        await TreeRoot.Apply(context);
        
        if (canceledByEffect)
        {
            Cancel();
            return false;
        }

        _abilityStat.Cooldown = _cooldown;
        _abilityStat.OnCooldownChanged?.Invoke();
        _state = State.Inactive;
        _getCastableRangeTask?.TrySetResult(result: true);
        _getCastableRangeTask = null;
        return true;
        
    }

    private Entity GetAffectTarget(Vector3 location)
    {
        var cell = PathFinder.Instance.GetCell(location.x, location.y);
        return cell?.Logical as Entity;
    }
    
    public async UniTask<bool> Execute(List<Vector3Int> affectTargets, Vector3 castPosition)
    {
        _castableRange = null;
        _state = State.Execution;
        GridIndicatorManager.Instance.HideAbilityPreview();

        var canceledByEffect = false;

        switch (_targetType)
        {
            case AbilityTargetType.None:
            case AbilityTargetType.Self:
            case AbilityTargetType.EmptyGround:
            {
                var context = new AbilityContext
                {
                    Owner = _owner,
                    Target = null,
                    Ability = this,
                    Position = castPosition,
                    Cancel = OnContextCancel,
                };

                await TreeRoot.Apply(context);
            }
                break;
            case AbilityTargetType.Enemy:
            case AbilityTargetType.Any:
                var effectTasks = Enumerable.Select(affectTargets.Select(location => new AbilityContext
                    {
                        Owner = _owner, Target = GetAffectTarget(location), Ability = this, Position = location,
                        Cancel = OnContextCancel,
                    }), context => TreeRoot.Apply(context))
                    .ToList();
                /*try
                {*/
                    await UniTask.WhenAll(effectTasks);
                /*}*/
                /*catch (Exception exception)
                {
                    Debug.Log(exception.Message);
                }*/

                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (canceledByEffect)
        {
            Cancel();
            return false;
        }

        _abilityStat.Cooldown = _cooldown;
        _abilityStat.OnCooldownChanged?.Invoke();
        _state = State.Inactive;
        _getCastableRangeTask?.TrySetResult(result: true);
        _getCastableRangeTask = null;
        return true;

        void OnContextCancel()
        {
            canceledByEffect = true;
        }
    }

    public bool IsSelecting()
    {
        return _state == State.Selection;
    }

    public void Cancel()
    {
        GridIndicatorManager.Instance.HideAbilityPreview();
        _state = State.Inactive;
        _getCastableRangeTask?.TrySetResult(result: false);
        _getCastableRangeTask = null;
        _castableRange = null;
    }

    public bool Available()
    {
        return true;
    }
    
}