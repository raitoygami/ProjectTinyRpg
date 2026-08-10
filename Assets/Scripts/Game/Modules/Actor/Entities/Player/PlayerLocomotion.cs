using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JSAM;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public partial class Player
{
    private Vector3 _lastCursorPosition;
    private List<PathNode> _pathNodes = new();
    private bool _keyboardInputEnabled;
    // Update is called once per frame
    private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[32];
    
    private async UniTask OnPointerMoveEvt(InputManager.PointerMoveEvt args)
    {
        // 背包拾起块：左键用于放下/丢弃，不绘制地面移动路径预览

        // 勿在此调用 IsPointerOverGameObject()：本方法由 Input 事件链触发，该 API 会读上一帧 UI 状态并报警告。
        // 指针在 UI 上时的 ClearPath 由 Player.Update 里的 DoNotClickUIAndGameAtSameTime 处理。

        if (!InputManager.Instance.IsKeyboardMouse())
        {
            GridIndicatorManager.Instance.HideCursorMark();
            return;
        }
        
        if (!GetPointerInput(out var hitPoint))
            return;
        var cursorGridPosition = hitPoint.SnapToGrid();

        if (cursorGridPosition != _lastCursorPosition)
        {
            GridIndicatorManager.Instance.ClearAffectableRange();
            if (_isPreparingAbility && _abilityPrepared != null)
            {
                /*GridIndicatorManager.Instance.HideCursorMark();*/

                // 这里显示技能
                var castableRange = AbilityUtil.GetCastableRange(_abilityPrepared, this);
                var abilityAffectRange = AbilityUtil.GetAbilityAffectRange(_abilityPrepared, this, castableRange, cursorGridPosition);
                GridIndicatorManager.Instance.ShowAffectableRange(abilityAffectRange);
            }
            
            var path = m_AgentMover.FindPath(cursorGridPosition, Const.Layer.ObstacleForNavi);
            GridIndicatorManager.Instance.DrawCursorMark(cursorGridPosition, path is { Count: > 0 }); 
        }
        
        // raycast for loot
        var hitCount = Physics2D.RaycastNonAlloc(
            hitPoint, // 起点（鼠标在世界空间中的位置）
            Vector2.zero, // 方向（零向量表示检测该点处的所有碰撞体，相当于 Point 检测）
            _hitBuffer, // 缓存数组
            100, // 最大检测距离（实际作用有限，因为方向为零）
            Const.Layer.ForInteractHover // 层遮罩
        );
        
        _interactableHovered?.OnHoverExit();
        _interactableHovered = null;
        if (hitCount > 0)
        {
            var hit = _hitBuffer[0];
            
            _interactableHovered = hit.collider.GetComponent<IInteractable>();
            _interactableHovered.OnHoverEnter();
        }

        _lastCursorPosition = cursorGridPosition;
        
        await UniTask.CompletedTask;
    }

    private async UniTask OnMouseClickEvt(InputManager.MouseClickEvt arg)
    {
        if (_isExecutingAbility)
            return;
        
        // 拾起背包块时左键由 InventoryTetris 全局处理（放下/丢弃），不应触发地面寻路与移动
        /*if (arg.mouseIndex == 0 && TetrisHandle.Instance.IsDragging())
            return;*/

        switch (arg.mouseIndex)
        {
            case 1:
                if (_isPreparingAbility && _abilityPrepared != null)
                {
                    _abilityPrepared.Cancel();
                    _ = OnPointerMoveEvt(null);
                }

                break;
            case 0:
                if (_interactableHovered != null)
                {
                    var go = _interactableHovered as MonoBehaviour;
                    if (go?.transform.SnapToGrid().Dist(GridPosition) <= 2)
                    {
                        _interactableHovered.OnInteract().Forget();
                        _interactableHovered = null;
                        return;
                    }
                }

                if (_isPreparingAbility && _abilityPrepared != null)
                {
                    ExecuteAbility(_abilityPrepared).Forget();
                    return;
                }
                
                MovePathAsync().Forget();

                break;
        }

        await UniTask.CompletedTask;
    }


    private async UniTask MovePathAsync()
    {
        if (_isPreparingAbility)
            return;

        ClearPath();

        if (!_Controllable)
            await UniTask.WaitUntil(() => _Controllable);
        if (!GetPointerInput(out var hitPoint))
            return;

        var targetPoint = hitPoint.SnapToGrid();
        if (targetPoint != GridPosition)
            _pathNodes = m_AgentMover.FindPath(targetPoint, Const.Layer.ObstacleForNavi);
        
        if (_pathNodes is { Count: > 0 })
            HandlePath().Forget();
    }

    private void ClearPath()
    {
        _pathNodes?.Clear();
    }

    private async UniTask HandlePath()
    {
        if (!_Controllable)
            await UniTask.WaitUntil(() => _Controllable);
        _Controllable = false;

        if (_pathNodes.Count <= 0)
            return;

        UIRoot.Instance.CloseLootPanel();

        var nextStep = _pathNodes[0];
        var finalPoint = _pathNodes[^1];
        _pathNodes.RemoveAt(0);

        var decision = DetermineMovement(nextStep, finalPoint);
        
        switch (decision.Result)
        {
            case MovementResult.Attack:
                ClearPath();
                await ExecuteWepAbility(m_AgentAbilities.GetWepAbility(), decision.AttackTargets, finalPoint.GetLocation());
                return;
            case MovementResult.None:
                ClearPath();
                await GetComponent<AgentAnimations>().PlayBump(nextStep.GetLocation());
                await UniTask.DelayFrame(2);
                ResetInput();
                return;
            case MovementResult.Move:
                await m_AgentMover.Move(nextStep.GetLocation());
                PlayStepSound(nextStep.GetLocation());
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

    private void PlayStepSound(Vector3 location)
    {
        AudioManager.PlaySound(GameAudioSounds.Sfx_Common_StepDirt);
    }
    
    private void ResetInput()
    {
        _Controllable = true;
        _keyboardInputEnabled = true;
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
        public readonly MovementResult Result;
        public readonly List<Vector3Int> AttackTargets; // 仅在 Attack 时有效

        public MovementDecision(MovementResult result, List<Vector3Int> affectTargets = null)
        {
            Result = result;
            AttackTargets = affectTargets ?? new List<Vector3Int>();
        }
    }

    private MovementDecision DetermineMovement(PathNode targetLocation, PathNode targetFinal)
    {
        // 1. 优先判断是否可以攻击（使用最终目标位置）
        var attackTargets = m_AgentAbilities.GetTargetByMove(this, targetFinal);
        if (attackTargets.Count > 0 && m_AgentAbilities.WithinBaseAttack(targetFinal))
        {
            return new MovementDecision(MovementResult.Attack, attackTargets);
        }

        // 2. 判断是否可以正常移动
        if (m_AgentMover.Moveable(targetLocation))
        {
            return new MovementDecision(MovementResult.Move);
        }

        // 3. 判断是否可以交互
        if (m_AgentInteractive.Interactable(targetLocation))
        {
            return new MovementDecision(MovementResult.Interaction);
        }

        return new MovementDecision(MovementResult.None);
    }

    private UniTask MoveStartEvent(AgentMover.MoveStartEvent arg)
    {
        if (arg.Forced)
            return UniTask.CompletedTask;

        m_AgentAnimations.FaceTarget(arg.TargetPosition - arg.StartPosition);
        m_AgentAnimations.BounceOnMove(arg.Duration);
       // m_AgentAnimations.Roll(arg.TargetPosition - arg.StartPosition, arg.Duration);
        return UniTask.CompletedTask;
    }

    private UniTask MoveForcedFinishEvent(AgentMover.MoveForcedFinishEvent arg)
    {
        var playerLocation = PlayerManager.Instance.GetLocation(); 
        playerLocation.CurrentLocation = arg.CurrPosition;
        playerLocation.CurrentDirection = m_AgentAnimations.GetDirection();
        
        var sceneName = SceneManager.GetActiveScene().name;
        var mapInfo = MapManager.Instance.GetMapInfo(sceneName);
        
        // 如果当前在大地图上， 同步大地图数据位置， 方便后续从地牢出来以后回到原本位置
        if (mapInfo is { MapType: MapConfig.MapType.WorldChunk })
        {
            playerLocation.CurrentWorldLocation = arg.CurrPosition;
            playerLocation.CurrentWorldDirection = m_AgentAnimations.GetDirection();
        }
        
        FOVManager.Instance.FovCompute(sceneName, GridPosition, 7);
        
        return UniTask.CompletedTask;
    }
    
    public async UniTask MoveFinishEvent(AgentMover.MoveFinishEvent arg)
    {
        var playerLocation = PlayerManager.Instance.GetLocation(); 
        playerLocation.CurrentLocation = arg.CurrPosition;
        playerLocation.CurrentDirection = m_AgentAnimations.GetDirection();
        
        var sceneName = SceneManager.GetActiveScene().name;
        var mapInfo = MapManager.Instance.GetMapInfo(sceneName);
        
        // 如果当前在大地图上， 同步大地图数据位置， 方便后续从地牢出来以后回到原本位置
        if (mapInfo is { MapType: MapConfig.MapType.WorldChunk })
        {
            playerLocation.CurrentWorldLocation = arg.CurrPosition;
            playerLocation.CurrentWorldDirection = m_AgentAnimations.GetDirection();
        }
        
        FOVManager.Instance.FovCompute(sceneName, GridPosition, 7);
        
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

        /*var nextPosition = GridPosition;
        if (Mathf.Abs(m_InputDirection.x) > 0 )
            nextPosition += new Vector3(m_InputDirection.x, 0, 0);
        else
            nextPosition += new Vector3(0, m_InputDirection.y, 0);
            */
        
        var nextPosition = GridPosition + new Vector3(m_InputDirection.x, m_InputDirection.y, 0);
        var target = new PathNode(nextPosition.x, nextPosition.y, true);

        _keyboardInputEnabled = false;

        SetPath(new List<PathNode> { target });
        return true;
    }

    private void SetPath(List<PathNode> value)
    {
        _pathNodes = value;
        if (_pathNodes.Count > 0)
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
            if (GridIndicatorManager.HasInstance())
                GridIndicatorManager.Instance.HideCursorMark();
            InputManager.Instance.MouseDisable();
        }
        else
        {
            InputManager.Instance.MouseEnable();
        }
    }
}