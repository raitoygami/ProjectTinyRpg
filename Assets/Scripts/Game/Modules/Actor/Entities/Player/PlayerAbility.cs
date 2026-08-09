using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class Player
{
    private bool _preparingAbility;
    private bool _executingAbility;
    private Ability _abilityPrepared;

    /// <summary>在指针移动且格子变化时更新技能范围预览（预备施法时）。</summary>
    private void UpdateSkillRangePreview(Vector3 hitPoint, Vector3 targetGrid)
    {
        var skillDisplay = _abilityPrepared != null ? _abilityPrepared.GetSkillDisplayParam() : null;
        var showSkillPreview = _abilityPrepared != null && _abilityPrepared.IsSelecting() && skillDisplay != null;
        if (showSkillPreview && InputManager.HasInstance() && GridIndicatorManager.HasInstance())
        {
            var mouseGrid = hitPoint.SnapToGrid();
            if (_abilityPrepared.TryGetSkillPreviewFrame(GridPosition, mouseGrid, out var previewOrigin,
                    out var skillFace))
            {
                GridIndicatorManager.Instance.ShowSkillRangePreview(skillDisplay, GridPosition, previewOrigin, skillFace,
                    Const.Layer.ObstacleForNavi);
            }
            else
            {
                GridIndicatorManager.Instance.HideSkillRangePreview();
            }
        }
        else if (GridIndicatorManager.HasInstance())
        {
            GridIndicatorManager.Instance.HideSkillRangePreview();
        }
    }

    public async UniTask PrepareAbility(Ability ability)
    {
        if (!ability.Available())
        {
            Debug.Log("Skill can't use.");
            return;
        }

        if (!m_AgentMover.IsMoving() && _Controllable)
        {
            _Controllable = false;
            ClearPath();

            _preparingAbility = true;
            _abilityPrepared = ability;
            var success = await ability.Select();
            _preparingAbility = false;

            _Controllable = true;
            _abilityPrepared = null;
        }
    }

    private async UniTask ExecuteAbility(Ability ability, List<Vector3> affectTargets, Vector3 targetPoint)
    {
        _executingAbility = true;
        
        if (await ability.Execute(affectTargets, targetPoint))
        {
            GetComponent<TurnActor>().FinishTurn();
        }

        _executingAbility = false;
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
        ability = ability == null ? m_AgentAbilities.GetWepAbility() : ability;
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
            if (!ability.SelectionRange(location).Contains(castPoint) ||
                !m_AgentAbilities.GetAffectTarget(location, ability, out affectTarget))
            {
                ability.Cancel();
                return;
            }
        }

        _executingAbility = true;
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
        
 
        _executingAbility = false;
    }
}
