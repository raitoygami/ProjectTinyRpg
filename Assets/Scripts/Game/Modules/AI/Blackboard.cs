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
        return _prepareRemined > 0;
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
        if (_target.GridPosition.Dist(_owner.GridPosition) <= wepAbility.GetRange())
        {
            _abilitySelect = wepAbility;
            _prepareRemined = _abilitySelect.GetPrepareTurn();
            return true;
        }
        
        return false;
    }

    public async UniTask<bool> Prepare()
    {
        var agentAbility = _owner.GetComponent<AgentAbilities>();
        var targetPoint = _target.GridPosition;

        /*if (!_abilitySelect.Available())
            return false;
            */
        
        _prepareRemined = 0;
        _hasPrepared = true;
        _targetOffset = _target.GridPosition - _owner.GridPosition;
        return await Prepare(_abilitySelect);

    }

    private async UniTask<bool> Prepare(Ability t_Ability)
    {
        Debug.Log("Preparing...");
        var agentAnimations = _owner.GetComponent<AgentAnimations>();
        agentAnimations.FaceTarget(_targetOffset);
        return true;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async UniTask<bool> UseAbility()
    {
        List<Entity> targets = null;
        var agentAbility = _owner.GetComponent<AgentAbilities>();
        var targetPoint = _hasPrepared ? _owner.GridPosition + _targetOffset : _target.GridPosition;

        if (_abilitySelect.IsTargeted())
        {
            var selectionPoint = new Vector2Int((int) targetPoint.x, (int) targetPoint.y);
            if (!_abilitySelect.SelectionRange().Contains(selectionPoint) ||
                !agentAbility.GetTargets(targetPoint, _abilitySelect, ref targets))
            {
                // 如果没有找到目标
                if (_hasPrepared)
                {
                    var node = PathFinder.Instance.GetCell(targetPoint.x, targetPoint.y);
                    var entity = node?.Logical as Entity;
                    await _abilitySelect.ExecuteMiss(targetPoint, entity);
                    _hasPrepared = false;
                    _targetOffset = Vector3.zero;
                    _abilitySelect = null;
                    return true;
                }

                Debug.Log($"Wrong Target {_abilitySelect.TargetMode()} - {!agentAbility.GetTargets(targetPoint, _abilitySelect, ref targets)}");
                _abilitySelect.Cancel();
                return false;
            }
        }
        
        await _abilitySelect.Execute(targets, targetPoint);

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
}