using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BattleManager : Singleton<BattleManager>
{

    public class BattleStartedEvent : EventArgs
    {
        
    }
    private BattleStartedEvent _BattleStartedEvent;
    public class BattleEndedEvent : EventArgs
    {
        
    }
    private BattleEndedEvent _BattleEndedEvent;
    //private List<Entity>
    // 当前正在锁定玩家（仇恨目标为玩家）的敌人列表
    private readonly HashSet<Entity> _enemiesTargetingPlayer = new();

    // 是否处于战斗状态
    public bool IsInBattle => _enemiesTargetingPlayer.Count > 0;

    public override void Initialized()
    {
        _BattleStartedEvent = new BattleStartedEvent();
        _BattleEndedEvent = new BattleEndedEvent();
    }


    public void AddEnemyTarget(Entity enemy)
    {
        if (_enemiesTargetingPlayer.Add(enemy))
        {
            // 如果从无到有，触发战斗开始事件
            if (_enemiesTargetingPlayer.Count == 1)
                OnBattleStarted();
        }
    }

    public void RemoveEnemyTarget(Entity enemy)
    {
        if (_enemiesTargetingPlayer.Remove(enemy))
        {
            // 如果从有到无，触发战斗结束事件
            if (_enemiesTargetingPlayer.Count == 0)
                OnBattleEnded();
        }
    }

    public void ClearEnemiesTargetingPlayer()
    {
        _enemiesTargetingPlayer.Clear();
        OnBattleEnded();
    }

    private void OnBattleStarted()
    {
        this.PublishGlobal(_BattleStartedEvent);
    }

    private void OnBattleEnded()
    {
        this.PublishGlobal(_BattleEndedEvent);
    }
    
}