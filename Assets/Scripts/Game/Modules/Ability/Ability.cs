using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.Localization;

[CreateAssetMenu(menuName = "Ability/Ability", fileName = "New Ability")]
public partial class Ability : ScriptableObject
{
    [Serializable]
    public enum AbilityTargetMode
    {
        None,
        Self,
        Enemy,
        Any,
        EmptyGround,
    }

    [SerializeField] public LocalizedString AbilityName;
    [SerializeField] public Sprite Icon;

    public WeaponType WeaponTypeRequire;
    
    // config
    [SerializeField] private int m_Range;

    [Tooltip("仅用于地面技能范围预览（与施法范围 m_Range 独立）；未配置则不绘制")] [SerializeReference] [SerializeField]
    private SelectParam m_SkillDisplayParam;

    [SerializeField] private AbilityTargetMode m_TargetMode;

    [SerializeField] private int m_CoolDown;

    //  cost
    [SerializeField] private int m_CostMP;
    [SerializeField] private int m_CostSP;

    public AbilityEffect TreeRoot;
    [SerializeField] public List<AbilityEffect> Effects = new();

    // internal state
    private enum State
    {
        Inactive,
        Selection,
        Execution
    }

    private State _State = State.Inactive;
    private Entity _Onwer;
    private int _CoolDown = 0;

    private UniTaskCompletionSource<bool> _SelectionTask;

    public int GetRange()
    {
        return m_Range;
    }

    /// <summary>技能范围显示用参数（扇形/矩形/圆等），仅地面高亮；目标判定在子 Effect（如 <c>E_AOE</c>）中完成。</summary>
    public SelectParam GetSkillDisplayParam()
    {
        return m_SkillDisplayParam;
    }

    public AbilityTargetMode TargetMode()
    {
        return m_TargetMode;
    }

    public bool IsTargeted()
    {
        return m_TargetMode != AbilityTargetMode.None && m_TargetMode != AbilityTargetMode.EmptyGround;
    }

    public void SetOnwer(Entity t_Owner)
    {
        if (t_Owner == _Onwer) return;
        _Onwer = t_Owner;
        t_Owner.Subscribe<TurnActor.TurnStartedEvent>(OnTurn);
    }

    private UniTask OnTurn(TurnActor.TurnStartedEvent args)
    {
        if (_CoolDown > 0)
        {
            _CoolDown--;
        }

        return UniTask.CompletedTask;
    }

    private List<Vector2Int> _SelectionRange;

    public List<Vector2Int> SelectionRange(bool t_Force = false)
    {
        if (_SelectionRange == null || t_Force)
        {
            _SelectionRange =
                TileSelector.Instance.Select( m_Range, _Onwer, false);
        }
        return _SelectionRange;
    }

    /// <summary>格点是否在施法范围内（与 <see cref="SelectionRange"/> 一致，由 <see cref="GetRange"/> 与寻路掩码计算）。</summary>
    public bool IsGridInCastRange(Vector3 gridPosition)
    {
        if (_Onwer == null)
            return false;
        var p = new Vector2Int((int)gridPosition.x, (int)gridPosition.y);
        return SelectionRange().Contains(p);
    }

    /// <summary>
    /// 技能范围预览用：沿玩家→鼠标路径在 <see cref="SelectionRange"/>（<see cref="GetRange"/>）内取最远格为起点；
    /// <paramref name="skillFaceDirection"/> 为玩家格→鼠标格（用于扇形/矩形朝向）；同格时为 <see cref="Vector3.forward"/>。
    /// </summary>
    public bool TryGetSkillPreviewFrame(Vector3 ownerGrid, Vector3 mouseGrid, out Vector3 previewOriginGrid,
        out Vector3 skillFaceDirection)
    {
        previewOriginGrid = default;
        skillFaceDirection = default;
        if (_Onwer == null)
            return false;

        var owner = ownerGrid.Round();
        var mouse = mouseGrid.Round();

        skillFaceDirection = WorldExtensions.GridDeltaXZ(owner, mouse);
        if (skillFaceDirection.sqrMagnitude < 1e-8f)
            skillFaceDirection = Vector3.up;

        var line = owner.LineTo(mouse);
        Vector3? lastInRange = null;
        foreach (var step in line)
        {
            var g = step.Round();
            if (IsGridInCastRange(g))
                lastInRange = g;
        }

        if (lastInRange == null)
            return false;

        previewOriginGrid = lastInRange.Value;
        return true;
    }

    public UniTask<bool> Select(bool t_ShowRange = true)
    {
        _SelectionTask = new UniTaskCompletionSource<bool>();
        if (_State != State.Inactive)
        {
            _SelectionTask.TrySetResult(result: false);
            return _SelectionTask.Task;
        }

        if (_CoolDown <= 0)
        {
            _State = State.Selection;
            _SelectionRange = TileSelector.Instance.Select(m_Range,_Onwer, t_ShowRange);
            return _SelectionTask.Task;
        }

        _SelectionTask.TrySetResult(result: false);
        return _SelectionTask.Task;
    }

    public async UniTask<bool> Execute(List<Entity> t_Targets, Vector3 t_Position)
    {
        _SelectionRange = null;
        _State = State.Execution;
        TileSelector.Instance.Hide();

        var canceledByEffect = false;

        void OnContextCancel()
        {
            canceledByEffect = true;
        }

        switch (m_TargetMode)
        {
            case AbilityTargetMode.None:
            case AbilityTargetMode.Self:
            case AbilityTargetMode.EmptyGround:
            {
                var context = new AbilityContext
                {
                    Owner = _Onwer,
                    Target = null,
                    Ability = this,
                    Position = t_Position,
                    Cancel = OnContextCancel,
                };

                await TreeRoot.Apply(context);
            }
                break;
            case AbilityTargetMode.Enemy:
            case AbilityTargetMode.Any:
                var effectTasks = Enumerable.Select(t_Targets.Select(target => new AbilityContext
                    {
                        Owner = _Onwer, Target = target, Ability = this, Position = t_Position,
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

        _CoolDown = m_CoolDown;
        _State = State.Inactive;
        _SelectionTask?.TrySetResult(result: true);
        _SelectionTask = null;
        return true;
    }

    public bool InSelection()
    {
        return _State == State.Selection;
    }

    public void Cancel()
    {
        //DisableTileSelection();
        TileSelector.Instance.Hide();
        _State = State.Inactive;
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