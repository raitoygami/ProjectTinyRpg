using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class Player
{
    private bool _isPreparingAbility;
    private bool _isExecutingAbility;
    private Ability _abilityPrepared;

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

    private async UniTask ExecuteWepAbility(Ability ability, List<Vector3Int> affectTargets, Vector3 targetPoint)
    {
        _isExecutingAbility = true;
        
        if (await ability.Execute(affectTargets, targetPoint))
        {
            GetComponent<TurnActor>().FinishTurn();
        }

        _isExecutingAbility = false;
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

        await ExecuteAbility(ability, targetPoint);
    }

    // cast point 施法点
    private async UniTask ExecuteAbility(Ability ability, Vector3 location)
    {
        _abilityPrepared = null;
        
        List<Vector3Int> affectTargets = null;
        
        var castPoint = Vector3Int.FloorToInt(location);
        var castableRange = ability.GetCastableRange(location); 
        if (!castableRange.Contains(castPoint) ||
            !m_AgentAbilities.GetAffectTarget(ability, castPoint, castableRange, out affectTargets))
        {
            ability.Cancel();
            return;
        }
        if (ability.IsTargeted())
        {
            var hasTarget = affectTargets.Select(affectPoint => 
                PathFinder.Instance.GetCell(affectPoint.x, affectPoint.y)
                ).
                Any(cell => AbilityUtil.IsTarget(ability, this, cell));

            if (!hasTarget)
            {
                ability.Cancel();
                return;
            }
        }

        _isExecutingAbility = true;
        if (ability.TryGetSkillPreviewFrame(GridPosition, location, out var previewOrigin,
                out var skillFace))
        {
            if (await ability.Execute(affectTargets, previewOrigin))
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
