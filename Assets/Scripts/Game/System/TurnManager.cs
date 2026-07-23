using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

// game loop: 
public class TurnManager : Singleton<TurnManager>
{
    public class NewLoopEvt : EventArgs
    {
        public int LoopCount;
    }

    private readonly NewLoopEvt _NewLoopEvt = new();
    [SerializeField] private List<TurnActor> m_ActorRegister = new();
    private readonly List<TurnActor> m_ActorRemoved = new();

    private int _ActorIndex = -1;

    public override void Initialized()
    {
        _ActorIndex = -1;
        _Running = false;
    }

    public static void Register(TurnActor actor)
    {
        if (HasInstance())
        {
            _instance.RegisterInternal(actor);
        }
    }
    
    private void RegisterInternal(TurnActor actor)
    {
        if (m_ActorRemoved.Contains(actor))
        {
            m_ActorRemoved.Remove(actor);
        }
        if (m_ActorRegister.Contains(actor))
        {
            return;
        }

        m_ActorRegister.Add(actor);

        if (!_Running)
        {
            _ = LoopTurns();
        }
    }

    public static void UnRegister(TurnActor actor)
    {
        if (HasInstance())
        {
            _instance.UnRegisterInternal(actor);
        }
    }
    private void UnRegisterInternal(TurnActor actor)
    {
        var index = m_ActorRegister.IndexOf(actor);
        if (index == -1)
            return;

        if (index <= _ActorIndex)
        {
            if (!m_ActorRemoved.Contains(actor))
            {
                m_ActorRemoved.Add(actor);    
            }
            return;
        }

        m_ActorRegister.Remove(actor);
    }

    private void UnRegisterRemovedActors()
    {
        foreach (var actor in m_ActorRemoved)
        {
            m_ActorRegister.Remove(actor);
        }

        m_ActorRemoved.Clear();
    }

    /// <summary>
    /// 移除已销毁的 TurnActor。退出 Play 时列表里可能残留 Unity 假 null：内层不 await、Count 仍 &gt; 0，
    /// 外层 while(true) 会空转卡死编辑器；Sort 比较器访问已销毁 transform 也会抛错。
    /// </summary>
    private void PurgeDestroyedActors()
    {
        for (var i = m_ActorRegister.Count - 1; i >= 0; i--)
        {
            if (!m_ActorRegister[i])
                m_ActorRegister.RemoveAt(i);
        }

        for (var i = m_ActorRemoved.Count - 1; i >= 0; i--)
        {
            if (!m_ActorRemoved[i])
                m_ActorRemoved.RemoveAt(i);
        }
    }

    private void SortActorsByDistanceToPlayerAscending()
    {
        var player = Context.HasInstance() ? Context.Instance.PlayerInst : null;
        if (player == null || player.transform == null)
            return;
        var playerGrid = player.transform.position.SnapToGrid();

        m_ActorRegister.Sort((a, b) =>
        {
            if (a == null) return 1;
            if (b == null) return -1;
            var gridA = a.transform.position.SnapToGrid();
            var gridB = b.transform.position.SnapToGrid();
            int distA = playerGrid.Dist(gridA);
            int distB = playerGrid.Dist(gridB);
            return distA.CompareTo(distB);
        });
    }

    private int _LoopCount;

    /// <summary>
    /// 当前大回合索引，与每轮开始时 <see cref="NewLoopEvt.LoopCount"/> 一致（从 0 递增）。
    /// 任务等需「游戏内时刻」时请用 <see cref="GameTimeConverter.TurnRoundToGameMinutes"/> 换算。
    /// </summary>
    public int CurrentGameTime { get; private set; }

    //private bool StopLoop = false;
    private bool _Running;
    
    public async UniTask LoopTurns()
    {
        _Running = true;
        while (true)
        {
            // 停止 Play / 销毁管理器时立刻退出，避免 UniTask 在 Application.isPlaying=false 后仍空转占满主线程。
            if (!Application.isPlaying || !this)
            {
                _Running = false;
                return;
            }

            CurrentGameTime = _LoopCount;
            _NewLoopEvt.LoopCount = CurrentGameTime;
            _LoopCount++;
            await this.PublishGlobal(_NewLoopEvt);

            if (!Application.isPlaying || !this)
            {
                _Running = false;
                return;
            }

            SortActorsByDistanceToPlayerAscending();

            _ActorIndex = 0;
            while (_ActorIndex < m_ActorRegister.Count)
            {
                var actor = m_ActorRegister[_ActorIndex];
                if (actor != null && !m_ActorRemoved.Contains(actor))
                {
                    await actor.StartTurn();
                }
                _ActorIndex++;

                //await UniTask.DelayFrame(1);
            }

            UnRegisterRemovedActors();
            PurgeDestroyedActors();

            if (m_ActorRegister.Count > 0)
                continue;

            break;
        }

        _Running = false;
    }

    protected override void OnRelease()
    {
        
    }
}