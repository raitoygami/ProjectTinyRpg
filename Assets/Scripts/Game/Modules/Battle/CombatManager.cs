using System;
using System.Collections.Generic;
using JSAM;

public class CombatManager : Singleton<CombatManager>
{

    public static float CombatMusicDPM = 60;
    public static float CombatMusicMul = 1;
    
    public class CombatStartedEvent : EventArgs
    {
        
    }
    private CombatStartedEvent _combatStartedEvent;
    public class CombatEndedEvent : EventArgs
    {
        
    }
    private CombatEndedEvent _combatEndedEvent;
    //private List<Entity>
    // 当前正在锁定玩家（仇恨目标为玩家）的敌人列表
    private readonly HashSet<Entity> _enemiesTargetingPlayer = new();

    // 是否处于战斗状态
    public bool IsInBattle => _enemiesTargetingPlayer.Count > 0;

    public override void Initialized()
    {
        _combatStartedEvent = new CombatStartedEvent();
        _combatEndedEvent = new CombatEndedEvent();
    }


    public void AddEnemyTarget(Entity enemy)
    {
        if (_enemiesTargetingPlayer.Add(enemy))
        {
            // 如果从无到有，触发战斗开始事件
            if (_enemiesTargetingPlayer.Count == 1)
                OnCombatStarted();
        }
    }

    public void RemoveEnemyTarget(Entity enemy)
    {
        if (_enemiesTargetingPlayer.Remove(enemy))
        {
            // 如果从有到无，触发战斗结束事件
            if (_enemiesTargetingPlayer.Count == 0)
                OnCombatEnded();
        }
    }

    // 切换场景的时候用到
    public void ClearEnemiesTargetingPlayer()
    {
        _enemiesTargetingPlayer.Clear();
        OnCombatEnded(true);
    }

    private void OnCombatStarted()
    {
        CombatMusicDPM = 120;
        CombatMusicMul = 2;
        if (MapLoader.HasInstance())
            MapLoader.Instance.OnCombatStart(1);
        AudioManager.StopMusic(GameAudioMusic.Combat_Loop_01);
        AudioManager.FadeMusicIn(GameAudioMusic.Combat_Loop_01, 1);
        this.PublishGlobal(_combatStartedEvent);
    }

    public void OnCombatEnded(bool immediate = false)
    {
        var fadeTime = immediate ? 0.1f : 10f;
        AudioManager.FadeMusicOut(GameAudioMusic.Combat_Loop_01, fadeTime);
        CombatMusicDPM = 60;
        CombatMusicMul = 1;
        if (MapLoader.HasInstance())
            MapLoader.Instance.OnCombatEnded(fadeTime);
        this.PublishGlobal(_combatEndedEvent);
    }

    public void StopCombatMusic()
    {
        AudioManager.StopMusic(GameAudioMusic.Combat_Loop_01);
    }
    
}