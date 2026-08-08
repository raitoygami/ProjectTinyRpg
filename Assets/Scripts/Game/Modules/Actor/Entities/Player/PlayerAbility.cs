using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class Player
{
    private bool _PreparingAbility;
    private bool _ExecutingAbility;
    private Ability _AbilityPrepared;

    /// <summary>在指针移动且格子变化时更新技能范围预览（预备施法时）。</summary>
    private void UpdateSkillRangePreview(Vector3 hitPoint, Vector3 targetGrid)
    {
        var skillDisplay = _AbilityPrepared != null ? _AbilityPrepared.GetSkillDisplayParam() : null;
        var showSkillPreview = _AbilityPrepared != null && _AbilityPrepared.IsSelecting() && skillDisplay != null;
        if (showSkillPreview && InputManager.HasInstance() && GridIndicatorManager.HasInstance())
        {
            var mouseGrid = hitPoint.SnapToGrid();
            if (_AbilityPrepared.TryGetSkillPreviewFrame(GridPosition, mouseGrid, out var previewOrigin,
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

    public async UniTask PrepareAbility(Ability t_Ability)
    {
        if (!t_Ability.Available())
        {
            Debug.Log("Skill can't use.");
            return;
        }

        if (!m_AgentMover.IsMoving() && _Controllable)
        {
            _Controllable = false;
            ClearPath();

            _PreparingAbility = true;
            _AbilityPrepared = t_Ability;
            var success = await t_Ability.Select();
            _PreparingAbility = false;

            _Controllable = true;
            _AbilityPrepared = null;
        }
    }

    private async UniTask ExecuteAbility(Ability t_Ability, List<Entity> targets, Vector3 targetPoint)
    {
        _ExecutingAbility = true;
        
        if (await t_Ability.Execute(targets, targetPoint))
        {
            GetComponent<TurnActor>().FinishTurn();
        }

        _ExecutingAbility = false;
    }
    
    private async UniTask ExecuteAbility(Ability t_Ability, PathNode t_TargetPoint)
    {
        t_Ability = t_Ability == null ? m_AgentAbilities.GetWepAbility() : t_Ability;
        if (t_Ability == null)
            return;

        var targetPoint = new Vector3(t_TargetPoint.X, 0, t_TargetPoint.Y);
        await ExecuteAbilityInternal(t_Ability, targetPoint);
    }

    private async UniTask ExecuteAbility(Ability t_Ability)
    {
        t_Ability = t_Ability == null ? m_AgentAbilities.GetWepAbility() : t_Ability;
        if (t_Ability == null)
            return;

        if (!GetPointerInput(out var hitPoint))
        {
            t_Ability.Cancel();
            return;
        }

        var targetPoint = hitPoint.SnapToGrid();

        await ExecuteAbilityInternal(t_Ability, targetPoint);
    }

    private async UniTask ExecuteAbilityInternal(Ability t_Ability, Vector3 targetPoint)
    {
        _AbilityPrepared = null;
        List<Entity> targets = null;
        if (t_Ability.IsTargeted())
        {
            var selectionPoint = new Vector2Int((int)targetPoint.x, (int)targetPoint.z);
            if (!t_Ability.SelectionRange().Contains(selectionPoint) ||
                !m_AgentAbilities.GetTargets(targetPoint, t_Ability, ref targets))
            {
                t_Ability.Cancel();
                return;
            }
        }

        _ExecutingAbility = true;
        if (t_Ability.TryGetSkillPreviewFrame(GridPosition, targetPoint, out var previewOrigin,
                out var skillFace))
        {
            if (await t_Ability.Execute(targets, previewOrigin))
            {
                GetComponent<TurnActor>().FinishTurn();
            }
        }
        else
        {
            t_Ability.Cancel();
        }
        
 
        _ExecutingAbility = false;
    }
}
