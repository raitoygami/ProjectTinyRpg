using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public partial class Player
{
    private List<PathNode> _Path = new();
    private LootUnit _lootUnitTarget;

    private Vector3 _LastGrid = Vector3.one;

    private bool keyboardInputEnabled;

    private async UniTask OnPointerMoveEvt(InputSystem.PointerMoveEvt t_Args)
    {
        // 背包拾起块：左键用于放下/丢弃，不绘制地面移动路径预览
        if (TetrisHandle.Instance.IsDragging())
        {
            if (TileSelector.HasInstance())
            {
                TileSelector.Instance.ClearPath();
                TileSelector.Instance.HideSkillRangePreview();
            }

            return;
        }

        // 勿在此调用 IsPointerOverGameObject()：本方法由 Input 事件链触发，该 API 会读上一帧 UI 状态并报警告。
        // 指针在 UI 上时的 ClearPath 由 Player.Update 里的 DoNotClickUIAndGameAtSameTime 处理。

        if (!GetPointerInput(out var hitPoint))
        {
            TileSelector.Instance.HideSkillRangePreview();
            return;
        }

        /*var targetGrid = hitPoint.SnapToGrid();
        if (!_LastGrid.Equals(targetGrid))
        {
            UpdateSkillRangePreview(hitPoint, targetGrid);

            _LastGrid = targetGrid;
            var node = PathFinder.Instance.GetNode(targetGrid.x, targetGrid.z);
            var loot = DropSystem.Instance.GetLootUnit((int)targetGrid.x, (int)targetGrid.z);
            if (Navigation.IsWalkable(node, Const.Layer.ForSelector))
            {
                TileSelector.Instance.DrawPath(
                    new List<NavigationNode>() { new(targetGrid.x, targetGrid.z, false) });
            }
            else
            {
                TileSelector.Instance.ClearPath();
            }

            /*var coverTarget = node.Reference != null ? node.Reference.gameObject :
                loot != null ? loot.gameObject : null;
            CoverTarget(_CoverTarget, false);
            if (coverTarget != null)
            {
                CoverTarget(coverTarget, true);
            }#1#
        }*/

        await UniTask.CompletedTask;
    }

    private void TargetToLoot()
    {
        _lootUnitTarget = null;

        if (!GetPointerInput(out var hitPoint))
            return;
        var targetGrid = hitPoint.SnapToGrid();
        if (_CoverTarget != null)
        {
            var entity = _CoverTarget.GetComponent<Entity>();
            if (entity != null && entity.GridPosition.Equals(targetGrid))
            {
                return;
            }
        }

        // _lootUnitTarget = DropSystem.Instance.GetLootUnit(targetGrid);
    }

    private async UniTask OnMouseClickEvt(InputSystem.MouseClickEvt arg)
    {
        if (_ExecutingAbility)
        {
            return;
        }

        // 拾起背包块时左键由 InventoryTetris 全局处理（放下/丢弃），不应触发地面寻路与移动
        if (arg.mouseIndex == 0 && TetrisHandle.Instance.IsDragging())
            return;

        switch (arg.mouseIndex)
        {
            case 1:
                if (_PreparingAbility && _AbilityPrepared != null)
                {
                    _AbilityPrepared.Cancel();
                    _ = OnPointerMoveEvt(null);
                }

                break;
            case 0:

                TargetToLoot();

                if (_PreparingAbility && _AbilityPrepared != null)
                {
                    _ = ExecuteAbility(_AbilityPrepared);
                }
                else
                {
                    _ = MovePathAsync();
                }

                break;
        }

        await UniTask.CompletedTask;
    }

    private void SkipTurn()
    {
        if (_PreparingAbility)
            return;
        if (!GetPointerInput(out var hitPoint))
            return;

        hitPoint = new Vector2(Mathf.Round(hitPoint.x), Mathf.Round(hitPoint.y));
        var targetPoint = hitPoint.SnapToGrid();
        if (targetPoint == transform.position.SnapToGrid())
            GetComponent<TurnActor>().FinishTurn();
    }

    private async UniTask MovePathAsync()
    {
        if (_PreparingAbility)
        {
            return;
        }

        ClearPath();

        if (!_Controllable)
        {
            await UniTask.WaitUntil(() => _Controllable);
        }

        if (!GetPointerInput(out var hitPoint))
        {
            return;
        }

        var targetPoint = hitPoint.SnapToGrid();
        if (targetPoint != GridPosition)
        {
            _Path = m_AgentMover.FindPath(targetPoint, Const.Layer.ObstacleForNavi);
        }

        if (_Path is { Count: > 0 })
        {
            _ = HandlePath();
        }
    }

    private void ClearPath()
    {
        _Path?.Clear();
    }

    private async UniTask HandlePath()
    {
        if (!_Controllable)
            await UniTask.WaitUntil(() => _Controllable);
        _Controllable = false;

        if (_Path.Count <= 0)
            return;

        UIRoot.Instance.CloseLootPanel();

        var nextStep = _Path[0];
        var finalPoint = _Path[^1];
        _Path.RemoveAt(0);

        if (_lootUnitTarget != null)
        {
            var lootPosition = _lootUnitTarget.transform.SnapToGrid();
            if (lootPosition.Equals(finalPoint.GetLocation()))
            {
                if (lootPosition.Dist(nextStep.GetLocation()) <= 0)
                {
                    await UIRoot.Instance.Open(Const.KeyUI.Inventory);
                    await UIRoot.Instance.OpenLootPanel(_lootUnitTarget);
                    _Controllable = true;
                    return;
                }
            }
        }

        var decision = DetermineMovement(nextStep, finalPoint);
        switch (decision.Result)
        {
            case MovementResult.Attack:
                await ExecuteAbility(m_AgentAbilities.GetWepAbility(), decision.AttackTargets, finalPoint.GetLocation());
                ClearPath();
                return;
            case MovementResult.None:
                ClearPath();
                await GetComponent<AgentAnimations>().PlayBump(nextStep.GetLocation());
                await UniTask.DelayFrame(2);
                ResetInput();
                return;
            case MovementResult.Move:
                await m_AgentMover.Move(nextStep.GetLocation());
                if (!IsInCombatMode) return;
                ClearPath();
                if (TileSelector.HasInstance())
                    TileSelector.Instance.ClearPath();

                return;
            case MovementResult.Interaction:
                ClearPath();
                await m_AgentInteractive.Interact(nextStep);
                GetComponent<TurnActor>().FinishTurn();
                return;
            default:
                return;
        }
    }

    private void ResetInput()
    {
        _Controllable = true;
        keyboardInputEnabled = true;
    }

    public enum MovementResult
    {
        None,
        Move,
        Attack,
        Interaction,
    }

    public struct MovementDecision
    {
        public MovementResult Result;
        public List<Entity> AttackTargets; // 仅在 Attack 时有效

        public MovementDecision(MovementResult result, List<Entity> targets = null)
        {
            Result = result;
            AttackTargets = targets ?? new List<Entity>();
        }
    }

    private MovementDecision DetermineMovement(PathNode t_TargetLocation, PathNode t_TargetFinal)
    {
        // 1. 优先判断是否可以攻击（使用最终目标位置）
        var attackTargets = m_AgentAbilities.GetAttackableTargets(this, t_TargetFinal);

        if (attackTargets.Count > 0 && m_AgentAbilities.WithinBaseAttack(t_TargetFinal))
        {
            return new MovementDecision(MovementResult.Attack, attackTargets);
        }

        // 2. 判断是否可以正常移动
        if (m_AgentMover.Moveable(t_TargetLocation))
        {
            return new MovementDecision(MovementResult.Move);
        }

        // 3. 判断是否可以交互
        if (m_AgentInteractive.Interactable(t_TargetLocation))
        {
            return new MovementDecision(MovementResult.Interaction);
        }

        return new MovementDecision(MovementResult.None);
    }

    private UniTask MoveStartEvent(AgentMover.MoveStartEvent arg)
    {
        if (arg.Forced)
            return UniTask.CompletedTask;
        var rollEvt = new MainUI.RollEvt
        {
            Direction = arg.TargetPosition - arg.StartPosition,
            Duration = arg.Duration
        };
        this.PublishGlobal(rollEvt);
        m_AgentAnimations.FaceTarget(arg.TargetPosition - arg.StartPosition);
        m_AgentAnimations.BounceOnMove(arg.Duration);
       // m_AgentAnimations.Roll(arg.TargetPosition - arg.StartPosition, arg.Duration);
        return UniTask.CompletedTask;
    }

    public async UniTask MoveFinishEvent(AgentMover.MoveFinishEvent arg)
    {
        m_TurnActor.FinishTurn();
        await this.PublishGlobal(new Context.PlayerMoveFinishEvt());
        await UniTask.CompletedTask;
    }

    private bool HandleKeyboardControls()
    {
        if (m_InputDirection == Vector2.zero)
        {
            return false;
        }

        var nextPosition = GridPosition;
        if (Mathf.Abs(m_InputDirection.x) > 0 )
            nextPosition += new Vector3(m_InputDirection.x, 0, 0);
        else
            nextPosition += new Vector3(0, m_InputDirection.y, 0);
        
        //var nextPosition = GridPosition + new Vector3(m_InputDirection.x, 0, m_InputDirection.y);
        var target = new PathNode(nextPosition.x, nextPosition.y, true);

        keyboardInputEnabled = false;

        SetPath(new List<PathNode> { target });
        return true;
    }

    private void SetPath(List<PathNode> value)
    {
        _Path = value;
        if (_Path.Count > 0)
        {
            _ = HandlePath();
        }
    }

    private void DoNotClickUIAndGameAtSameTime()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        if (EventSystem.current.IsPointerOverGameObject())
        {
            // 指针在 UI 上时禁用对世界的鼠标采样，并清掉地面移动路径预览（避免禁用后 OnPointerMove 不再刷新）
            if (TileSelector.HasInstance())
                TileSelector.Instance.ClearPath();
            InputSystem.Instance.MouseDisable();
        }
        else
        {
            InputSystem.Instance.MouseEnable();
        }
    }
}