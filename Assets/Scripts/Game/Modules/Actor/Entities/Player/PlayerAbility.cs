using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class Player
{
    private bool _isPreparingAbility;
    private bool _isExecutingAbility;
    private Ability _abilityPrepared;

    public Ability GetAbilityPrepared()
    {
        return _abilityPrepared;
    }

    public async UniTask PrepareWepAtk(int abilityID)
    {
        var ability = await m_AgentAbilities.GetWepAtkAbility(abilityID);
        if (ability == null)
            return;

        if (ability == _abilityPrepared)
        {
            _abilityPrepared.Cancel();
            return;
        }
        
        _abilityPrepared?.Cancel();

        await PrepareAbility(ability);
            
    }
    
    public async UniTask PrepareAbility(Ability ability)
    {
        if (!ability.Available())
        {
            Debug.Log("Ability can't use.");
            return;
        }

        if (!m_AgentMover.IsMoving() && _Controllable)
        {
            _Controllable = false;
            ClearPath();

            _isPreparingAbility = true;
            _abilityPrepared = ability;
            await ability.PrepareCast(true);
            _isPreparingAbility = false;
            
            _Controllable = true;
            _abilityPrepared = null;
        }
    }

    private async UniTask ExecuteAbility(Ability ability, List<Vector3> affectTargets, Vector3 targetPoint)
    {
        _isExecutingAbility = true;
        
        if (await ability.Execute(affectTargets, targetPoint))
        {
            GetComponent<TurnActor>().FinishTurn();
        }

        _isExecutingAbility = false;
    }
    
    private async UniTask ExecuteAbility(Ability ability, PathNode t_TargetPoint)
    {
        ability = ability == null ? m_AgentAbilities.GetWepAbility() : ability;
        if (ability == null)
            return;

        var targetPoint = new Vector3(t_TargetPoint.X, 0, t_TargetPoint.Y);
        await ExecuteAbilityInternal(ability, targetPoint);
    }

    private async UniTask ExecuteAbility(Ability ability)
    {
        if (ability == null)
            return;
        if (!GetPointerInput(out var hitPoint))
        {
            ability.Cancel();
            return;
        }

        var targetPoint = hitPoint.SnapToGrid();

        await ExecuteAbilityInternal(ability, targetPoint);
    }

    // cast point 施法点
    private async UniTask ExecuteAbilityInternal(Ability ability, Vector3 location)
    {
        _abilityPrepared = null;
        
        List<Vector3> affectTarget = null;
        if (ability.IsTargeted())
        {
            var castPoint = new Vector3Int((int)location.x, (int)location.y, 0);
            if (!ability.GetCastableRange(location).Contains(castPoint) ||
                !m_AgentAbilities.GetAffectTarget(location, ability, out affectTarget))
            {
                ability.Cancel();
                return;
            }
        }

        _isExecutingAbility = true;
        if (ability.TryGetSkillPreviewFrame(GridPosition, location, out var previewOrigin,
                out var skillFace))
        {
            if (await ability.Execute(affectTarget, previewOrigin))
            {
                GetComponent<TurnActor>().FinishTurn();
            }
        }
        else
        {
            ability.Cancel();
        }
        
 
        _isExecutingAbility = false;
    }
}
