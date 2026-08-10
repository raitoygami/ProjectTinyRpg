using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Blackboard
{
    private readonly Entity _owner;

    private Entity _target;
    private Ability _abilitySelect;
    private List<Vector3Int> _telegraph;
    public Blackboard(Entity owner)
    {
        _owner = owner;
    }

    public void SetTarget(Entity e) => _target = e;

    public Entity Target => _target;

    public void ClearTargetOnly() => _target = null;

    public Entity GetOwer() => _owner;
    
    public async UniTask<bool> MoveTowardsGrid(Vector3 goalGrid)
    {
        if (_owner.GridPosition.Dist(goalGrid) <= 0)
            return true;

        var mover = _owner.GetComponent<AgentMover>();

        if (mover.IsMoving())
            await UniTask.WaitUntil(() => !mover.IsMoving());
        var path = mover.FindPath(goalGrid);
        if (path is not { Count: > 0 })
            return false;

        var nextPath = path[0];
        if (!mover.Moveable(nextPath))
            return false;

        _ = mover.Move(nextPath.GetLocation());
        return true;
    }

    private int _prepareRemined;
    private bool _hasPrepared;
    private Vector3 _targetOffset;
    public bool IsPreparing()
    {
        return _prepareRemined > 0 && _abilitySelect != null;
    }

    public bool HasPrepared()
    {
        return _hasPrepared;
    }
    
    public Ability GetAbilitySelected()
    {
        return _abilitySelect;
    }
    
    public bool SelectAbility()
    {
        if (_abilitySelect != null)
            return true;
        
        _abilitySelect = null;
        var agentAbility = _owner.GetComponent<AgentAbilities>();
        var wepAbility = agentAbility.GetWepAbility();
        if (wepAbility == null)
            return false;
        if (wepAbility.IsOnCooldown())
            return false;

        if (_target == null || _owner == null)
            return false;
        if (_target.transform == null)
            return false;
        
        // 这里获取的是武器技能
        if (_target.GridPosition.Dist(_owner.GridPosition) > wepAbility.GetCastRange()) return false;
        // 这里需要判断是否有 (目前实现了直线方向上的第一个目标)
        var range = AbilityUtil.GetAbilityPrepareRange(wepAbility, _owner, _target.GridPosition);
        var firstTarget = AbilityUtil.GetCloseTarget(_owner, wepAbility, range);
        if (firstTarget == null || !EntityManager.IsEnemyFraction(firstTarget.Faction, _owner.Faction))
            return false;
        _abilitySelect = wepAbility;
        _prepareRemined = _abilitySelect.GetPrepareTurn();
        return true;

    }

    public async UniTask<bool> Prepare()
    {
        var agentAbility = _owner.GetComponent<AgentAbilities>();
        var targetPoint = _target.GridPosition;

        /*if (!_abilitySelect.Available())
            return false;
            */
        _telegraph = AbilityUtil.GetAbilityPrepareRange(_abilitySelect, _owner, targetPoint);
        GridIndicatorManager.Instance.AddTelegraph(_telegraph.ToArray());
        _prepareRemined = 0;
        _hasPrepared = true;
        _targetOffset = _target.GridPosition - _owner.GridPosition;
        return await Prepare(_abilitySelect);

    }

    private async UniTask<bool> Prepare(Ability t_Ability)
    {
        var agentAnimations = _owner.GetComponent<AgentAnimations>();
        agentAnimations.FaceTarget(_targetOffset);
        await UniTask.CompletedTask;
        
        return true;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async UniTask<bool> UseAbility()
    {
        List<Vector3Int> affectTargets = null;
        var agentAbility = _owner.GetComponent<AgentAbilities>();
        var castPoint = _hasPrepared ? _owner.GridPosition + _targetOffset : _target.GridPosition;

        if (_abilitySelect.IsTargeted())
        {
            var selectionPoint = Vector3Int.FloorToInt(castPoint);
            var castableRange = _abilitySelect.GetCastableRange(castPoint); 
            
            if (!castableRange.Contains(selectionPoint) ||
                !agentAbility.GetAffectTarget(_abilitySelect, selectionPoint, castableRange, out affectTargets))
            {
                // 如果没有找到目标
                if (_hasPrepared && _telegraph != null)
                {
                    var entity = AbilityUtil.GetCloseTarget(_owner, _abilitySelect, _telegraph);
                    // 这里需要根据距离获取目标
                    await _abilitySelect.ExecuteMiss(castPoint, entity);
                    
                    GridIndicatorManager.Instance.RemoveTelegraph(_telegraph.ToArray());
                    _telegraph.Clear();
                    
                    _hasPrepared = false;
                    _targetOffset = Vector3.zero;
                    _abilitySelect = null;
                    return true;
                }

                _abilitySelect.Cancel();
                return false;
            }
        }
        
        await _abilitySelect.Execute(affectTargets, castPoint);
        if (_telegraph != null)
        {
            GridIndicatorManager.Instance.RemoveTelegraph(_telegraph.ToArray());
            _telegraph.Clear();    
        }
        
        _hasPrepared = false;
        _targetOffset = Vector3.zero;
        _abilitySelect = null; 
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async UniTask<bool> Follow()
    {
        var mover = _owner.GetComponent<AgentMover>();

        if (mover.IsMoving())
            await UniTask.WaitUntil(() => !mover.IsMoving());

        if (_target == null || _target.gameObject == null)
        {
            ClearTargetOnly();
            return false;
        }
        
        var path = mover.FindPath(_target.GridPosition);
        if (path is not {Count : > 0})
            return false;

        var nextPath = path[0];
        if (!mover.Moveable(nextPath))
        {
            return false;
        }

        _ =  mover.Move(nextPath.GetLocation());
        return true;
    }

    public void RefreshTelegraph()
    {
        if (!_hasPrepared || _telegraph == null) return;
        GridIndicatorManager.Instance.RemoveTelegraph(_telegraph.ToArray());
        var targetPoint = _owner.GridPosition + _targetOffset;
        _telegraph = AbilityUtil.GetAbilityPrepareRange(_abilitySelect, _owner, targetPoint);
        GridIndicatorManager.Instance.AddTelegraph(_telegraph.ToArray());
    }
    
    public void Clear()
    {
        if (_telegraph is { Count: > 0 })
        {
            if (GridIndicatorManager.HasInstance())
            {
                GridIndicatorManager.Instance.RemoveTelegraph(_telegraph.ToArray());
            }
            _telegraph.Clear();
        }
        _telegraph = null;
    }
    
}