using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

// game loop: 
public class TurnManager : Singleton<TurnManager>
{
    public class NewLoopEvt : EventArgs
    {
        public int LoopCount;
    }

    private readonly NewLoopEvt _NewLoopEvt = new();
    [SerializeField] private List<TurnActor> m_EntityRegister = new();
    private readonly List<TurnActor> m_EntityRemoved = new();

    private int _EntityIndex = -1;

    public override void Initialized()
    {
        _EntityIndex = -1;
        _Running = false;
    }

    public int GetEntityCount()
    {
        return m_EntityRegister.Count;
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
        if (m_EntityRemoved.Contains(actor))
        {
            m_EntityRemoved.Remove(actor);
        }
        if (m_EntityRegister.Contains(actor))
        {
            return;
        }

        m_EntityRegister.Add(actor);

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
        var index = m_EntityRegister.IndexOf(actor);
        if (index == -1)
            return;

        if (index <= _EntityIndex)
        {
            if (!m_EntityRemoved.Contains(actor))
            {
                m_EntityRemoved.Add(actor);    
            }
            return;
        }

        m_EntityRegister.Remove(actor);
    }

    private void UnRegisterRemovedActors()
    {
        foreach (var actor in m_EntityRemoved)
        {
            m_EntityRegister.Remove(actor);
        }

        m_EntityRemoved.Clear();
    }

    /// <summary>
    /// 移除已销毁的 TurnActor。退出 Play 时列表里可能残留 Unity 假 null：内层不 await、Count 仍 &gt; 0，
    /// 外层 while(true) 会空转卡死编辑器；Sort 比较器访问已销毁 transform 也会抛错。
    /// </summary>
    private void PurgeDestroyedActors()
    {
        for (var i = m_EntityRegister.Count - 1; i >= 0; i--)
        {
            if (!m_EntityRegister[i])
                m_EntityRegister.RemoveAt(i);
        }

        for (var i = m_EntityRemoved.Count - 1; i >= 0; i--)
        {
            if (!m_EntityRemoved[i])
                m_EntityRemoved.RemoveAt(i);
        }
    }

    private void SortActorsByDistanceToPlayerAscending()
    {
        var player = Context.HasInstance() ? Context.Instance.PlayerInst : null;
        if (player == null || player.transform == null)
            return;
        var playerGrid = player.transform.position.SnapToGrid();

        m_EntityRegister.Sort((a, b) =>
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
    private CancellationTokenSource _cts = new();
    
    public async UniTask LoopTurns()
    {
        // 如果已有运行中的循环，先取消旧的
        if (_Running)
        {
            _cts.Cancel();
            await UniTask.Yield(); // 给旧循环一点时间退出
        }
                
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        _Running = true;

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                if (!Application.isPlaying || !this)
                    break;

                CurrentGameTime = _LoopCount;
                _NewLoopEvt.LoopCount = CurrentGameTime;
                _LoopCount++;

                await this.PublishGlobal(_NewLoopEvt).AttachExternalCancellation(_cts.Token);

                if (_cts.Token.IsCancellationRequested) break;

                SortActorsByDistanceToPlayerAscending();

                _EntityIndex = 0;
                while (_EntityIndex < m_EntityRegister.Count)
                {
                    if (_cts.Token.IsCancellationRequested) break;

                    var actor = m_EntityRegister[_EntityIndex];
                    if (actor != null && !m_EntityRemoved.Contains(actor))
                    {
                        await actor.StartTurn().AttachExternalCancellation(_cts.Token);
                    }

                    _EntityIndex++;
                }

                UnRegisterRemovedActors();
                PurgeDestroyedActors();

                if (m_EntityRegister.Count == 0 || _cts.Token.IsCancellationRequested)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // 正常取消，不需要报错
        }
        finally
        {
            _Running = false;
        }
    }

    public void StopLoop()
    {
        _cts?.Cancel();
    }

    public void ClearAll()
    {
        m_EntityRegister.Clear();
        m_EntityRemoved.Clear();
    }
    
    protected override void OnRelease()
    {
        
    }
}