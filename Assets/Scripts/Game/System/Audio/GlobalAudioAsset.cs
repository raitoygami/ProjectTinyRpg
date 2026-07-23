using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ry/GlobalAudioAsset", fileName = "GlobalAudioAsset")]
public class GlobalAudioAsset : ScriptableObject{
    [SerializeField] private List<Audio> m_Musics;
    [SerializeField] private List<Audio> m_Sfxs;

    public List<Audio> Musics => m_Musics;
    public List<Audio> Sfxs => m_Sfxs;

}
