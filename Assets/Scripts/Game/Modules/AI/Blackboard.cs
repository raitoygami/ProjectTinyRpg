using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Blackboard
{
    private readonly Entity m_Onwer;

    private Entity _Target;
    private Ability _AbilitySelect;
    public Blackboard(Entity t_Owner)
    {
        m_Onwer = t_Owner;
    }

    public void SetTarget(Entity e) => _Target = e;

    public Entity Target => _Target;

    public void ClearTargetOnly() => _Target = null;

    public Entity GetOwer() => m_Onwer;
    
    /// <summary>向指定格子走一步（与 <see cref="Follow"/> 相同寻路逻辑，目标为格坐标而非实体）。</summary>
    public async UniTask<bool> MoveTowardsGrid(Vector3 goalGrid)
    {
        if (m_Onwer.GridPosition.Dist(goalGrid) <= 0)
            return true;

        var mover = m_Onwer.GetComponent<AgentMover>();

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniTask<bool> FindTarget(int range)
    {
        var enemies = EntityManager.Instance.FindEnemies(m_Onwer, range);
        if (enemies == null || enemies.Count == 0)
            return UniTask.FromResult(false);

        _Target = enemies.FirstOrDefault();
        return UniTask.FromResult(true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UniTask<bool> FindTarget() => FindTarget(8);

    public bool SelectAbility()
    {
        var agentAbility = m_Onwer.GetComponent<AgentAbilities>();
        var wepAbility = agentAbility.GetWepAbility();
        if (wepAbility == null)
            return false;
        if (wepAbility.isSkillOnCooldown())
            return false;

        if (_Target == null || m_Onwer == null)
            return false;
        if (_Target.transform == null)
            return false;
        if (_Target.GridPosition.Dist(m_Onwer.GridPosition) <= wepAbility.GetRange())
        {
            _AbilitySelect = wepAbility;
            return true;
        }
        
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async UniTask<bool> UseAbility()
    {
        
        var agentAbility = m_Onwer.GetComponent<AgentAbilities>();
        
        List<Entity> targets = null;

        var targetPoint = _Target.GridPosition;
        
        if (_AbilitySelect.IsTargeted())
        {
            var selectionPoint = new Vector2Int((int) targetPoint.x, (int) targetPoint.y);
            if (!_AbilitySelect.SelectionRange().Contains(selectionPoint) ||
                !agentAbility.GetTargets(targetPoint, _AbilitySelect, ref targets))
            {
                Debug.Log($"Wrong Target {_AbilitySelect.TargetMode()} - {!agentAbility.GetTargets(targetPoint, _AbilitySelect, ref targets)}");
                _AbilitySelect.Cancel();
                return false;
            }
        }
        return await _AbilitySelect.Execute(targets, targetPoint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async UniTask<bool> Follow()
    {
        var mover = m_Onwer.GetComponent<AgentMover>();

        if (mover.IsMoving())
            await UniTask.WaitUntil(() => !mover.IsMoving());

        if (_Target == null || _Target.gameObject == null)
        {
            ClearTargetOnly();
            return false;
        }
        
        var path = mover.FindPath(_Target.GridPosition);
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