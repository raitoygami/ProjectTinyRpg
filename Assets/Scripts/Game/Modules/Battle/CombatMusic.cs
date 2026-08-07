using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Config/Combat Music", fileName = "CombatMusic", order = 0)]
public class CombatMusic : ScriptableObject
{
    [Serializable]
    public class MusicInfo
    {
        public GameAudioMusic Music;
        public float BMP;
        public float Beat;
    }
    
    public List<MusicInfo> MusicInfos = new();

    public MusicInfo GetMusicInfo(GameAudioMusic music)
    {
        return MusicInfos.FirstOrDefault(musicInfo => musicInfo.Music == music);
    }
    
}
