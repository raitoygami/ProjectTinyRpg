using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class TurnActor : MonoBehaviour
{
    private static readonly WaitForEndOfFrame WaitEndOfFrame = new();

    public class TurnStartedEvent : EventArgs
    {
        public Entity Owner;

        /// <summary>
        /// 由 <see cref="TurnStartedEvent"/> 的 handler（如召唤物寿命到期）设为 true，表示本回合不应再执行
        /// <see cref="ProcessTurn"/>；<see cref="StartTurn"/> 在 await Publish 后读取，不依赖已销毁的 TurnActor。
        /// </summary>
        public bool AbortTurn;
    }

    public class TurnActionEvent : EventArgs
    {
    }

    public class TurnEndedEvent : EventArgs
    {
    }

    private TurnStartedEvent m_TurnStartedEvent;

    /// <summary>
    /// 若在父级 Awake 里 AddComponent 后立即设置 MaxActionPoint，Awake/OnEnable 时注册会早于该赋值，
    /// TurnManager 可能已开始 LoopTurns 并以默认 maxActionPoint 执行 StartTurn。注册延到 Start（及重启用）。
    /// </summary>
    private bool _registeredWithTurnManager;

    private void Awake()
    {
        m_TurnStartedEvent = new TurnStartedEvent
        {
            Owner = GetComponent<Entity>()
        };
    }

    private void Start()
    {
        _registeredWithTurnManager = true;
        TurnManager.Register(this);
    }

    private void OnEnable()
    {
        if (_registeredWithTurnManager)
        {
            TurnManager.Register(this);
        }
    }

    private void OnDisable()
    {
        TurnManager.UnRegister(this);
    }

    private void OnDestroy()
    {
        TurnManager.UnRegister(this);
        // 行动中若实体被 Destroy（如战斗中阵亡），异步 continuation 可能晚于本组件销毁；须解除 StartTurn 对 _PendingTurn 的等待，否则会卡住回合。
        _PendingTurn?.TrySetResult(true);
    }

    private UniTaskCompletionSource<bool> _PendingTurn;

    // Start is called before the first frame update
    public bool isActing { get; private set; }

    [SerializeField] private int maxActionPoint = 1;

    /// <summary>本回合最大行动点，回合开始时重置为当前值。</summary>
    public int MaxActionPoint
    {
        get => maxActionPoint;
        set => maxActionPoint = value;
    }

    /// <summary>当前剩余行动点（本回合内）。</summary>
    public int ActionPoint { get; private set; }

    public async UniTask StartTurn()
    {
        if (isActing)
        {
            return;
        }
        isActing = true;
        ActionPoint = maxActionPoint;

        _PendingTurn = new UniTaskCompletionSource<bool>();
        var turnStarted = m_TurnStartedEvent;
        turnStarted.AbortTurn = false;

        if (this.HasSubscription<TurnStartedEvent>())
        {
            // 顺序执行：后 Subscribe 的 handler 先执行（见 PubSub.sequential）。召唤物寿命在 ConfigureAsSummon 里最后订阅，
            // 会先于 Ability.OnTurn 等运行，避免并行 WhenAll 在 Expire→Destroy 后仍有 handler 访问已毁实体。
            await this.Publish(turnStarted, sequential: true);
        }

        // 寿命到期等：handler 在 Destroy 前设置 AbortTurn；continuation 里 this 可能已销毁，用堆上的 turnStarted 判断。
        if (turnStarted.AbortTurn)
        {
            isActing = false;
            _PendingTurn?.TrySetResult(true);
            return;
        }

        _ = ProcessTurn();

        await _PendingTurn.Task;
    }

    private async UniTask ProcessTurn()
    {

        if (this.HasSubscription<TurnActionEvent>())
        {
            _ = this.Publish(new TurnActionEvent());
        }
        else
        {
            FinishTurn();
        }

        await UniTask.CompletedTask;
    }

    public void FinishTurn()
    {

        if (!isActing)
        {
            return;
        }

        ActionPoint--;
        if (ActionPoint > 0)
        {
            ProcessTurn().Forget();
            return;
        }

        isActing = false;

        if (this.HasSubscription<TurnEndedEvent>())
        {
            this.Publish(new TurnEndedEvent());
        }

        _PendingTurn?.TrySetResult(result: true);
    }

    /// <summary>仅增加本回合剩余 ActionPoint，不改变 MaxActionPoint；非行动中无效。</summary>
    public void AddActionPoints(int delta)
    {

        if (delta == 0 || !isActing)
        {
            return;
        }

        ActionPoint = Mathf.Max(0, ActionPoint + delta);
    }
}