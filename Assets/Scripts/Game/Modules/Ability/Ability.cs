using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Ability/Ability", fileName = "New Ability")]
public partial class Ability : ScriptableObject
{

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
    private SelectParam m_SkillDisplayParam;

    
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
    
    private UniTaskCompletionSource<bool> _SelectionTask;

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

    /// <summary>技能范围显示用参数（扇形/矩形/圆等），仅地面高亮；目标判定在子 Effect（如 <c>E_AOE</c>）中完成。</summary>
    public SelectParam GetSkillDisplayParam()
    {
        return m_SkillDisplayParam;
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

    private List<Vector2Int> _SelectionRange;

    public List<Vector2Int> SelectionRange(bool force = false)
    {
        if (_SelectionRange == null || force)
        {
            _SelectionRange =
                AbilityUtil.CalculateRange( this, _owner);
        }
        return _SelectionRange;
    }

    /// <summary>格点是否在施法范围内（与 <see cref="SelectionRange"/> 一致，由 <see cref="GetCastRange"/> 与寻路掩码计算）。</summary>
    public bool IsGridInCastRange(Vector3 gridPosition)
    {
        if (_owner == null)
            return false;
        var p = new Vector2Int((int)gridPosition.x, (int)gridPosition.y);
        return SelectionRange().Contains(p);
    }

    /// <summary>
    /// 技能范围预览用：沿玩家→鼠标路径在 <see cref="SelectionRange"/>（<see cref="GetCastRange"/>）内取最远格为起点；
    /// <paramref name="skillFaceDirection"/> 为玩家格→鼠标格（用于扇形/矩形朝向）；同格时为 <see cref="Vector3.forward"/>。
    /// </summary>
    public bool TryGetSkillPreviewFrame(Vector3 ownerGrid, Vector3 mouseGrid, out Vector3 previewOriginGrid,
        out Vector3 skillFaceDirection)
    {
        previewOriginGrid = default;
        skillFaceDirection = default;
        if (_owner == null)
            return false;

        var owner = ownerGrid.Round();
        var mouse = mouseGrid.Round();

        skillFaceDirection = WorldExtensions.GridDeltaXZ(owner, mouse);
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

    public UniTask<bool> Select(bool t_ShowRange = true)
    {
        _SelectionTask = new UniTaskCompletionSource<bool>();
        if (_state != State.Inactive)
        {
            _SelectionTask.TrySetResult(result: false);
            return _SelectionTask.Task;
        }

        if (_abilityStat.Cooldown <= 0)
        {
            _state = State.Selection;
            _SelectionRange = AbilityUtil.CalculateRange(this, _owner);
            return _SelectionTask.Task;
        }

        _SelectionTask.TrySetResult(result: false);
        return _SelectionTask.Task;
    }

    public async UniTask<bool> ExecuteMiss(Vector3 position, Entity target)
    {
        _SelectionRange = null;
        _state = State.Execution;
        GridIndicatorManager.Instance.Hide();

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
        _SelectionTask?.TrySetResult(result: true);
        _SelectionTask = null;
        return true;
        
    }
    
    public async UniTask<bool> Execute(List<Entity> t_Targets, Vector3 t_Position)
    {
        _SelectionRange = null;
        _state = State.Execution;
        GridIndicatorManager.Instance.Hide();

        var canceledByEffect = false;

        void OnContextCancel()
        {
            canceledByEffect = true;
        }

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
                    Position = t_Position,
                    Cancel = OnContextCancel,
                };

                await TreeRoot.Apply(context);
            }
                break;
            case AbilityTargetType.Enemy:
            case AbilityTargetType.Any:
                var effectTasks = Enumerable.Select(t_Targets.Select(target => new AbilityContext
                    {
                        Owner = _owner, Target = target, Ability = this, Position = t_Position,
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
        _SelectionTask?.TrySetResult(result: true);
        _SelectionTask = null;
        return true;
    }

    public bool IsSelecting()
    {
        return _state == State.Selection;
    }

    public void Cancel()
    {
        //DisableTileSelection();
        GridIndicatorManager.Instance.Hide();
        _state = State.Inactive;
        _SelectionTask?.TrySetResult(result: false);
        _SelectionTask = null;
        _SelectionRange = null;
        /*this.Publish(new CancelEvent
        {
            skill = this
        });
        this.Publish(new ToggleEvent
        {
            skill = this,
            active = false
        });*/
    }

    public bool Available()
    {
        return true;
    }
}