/*
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioSystem : Singleton<AudioSystem>{
    private GlobalAudioAsset m_AudioAsset;
    private AudioSource m_MusicSourceIntro;
    private AudioSource m_MusicSourceLoop;
    
    private List<AudioSource> m_SfxSource = new();

    private void Awake(){
        m_MusicSourceIntro = new GameObject("Intro").AddComponent<AudioSource>();
        m_MusicSourceIntro.transform.SetParent(transform);
        m_MusicSourceLoop = new GameObject("Loop").AddComponent<AudioSource>();
        m_MusicSourceLoop.transform.SetParent(transform);
        
        for (var i = 0; i < 10; i++){
            var sfx = new GameObject($"Sfx-Channel{i:00}").AddComponent<AudioSource>();
            sfx.transform.SetParent(transform);
            sfx.playOnAwake = false;
            m_SfxSource.Add(sfx);
        }

        m_AudioAsset = Global.AudioAsset;
    }

    public float PlayIntro(string MusicName, double time){
        foreach (var music in m_AudioAsset.Musics.Where(music => music.Name == MusicName)){
            var len = music.Clip.length;// / music.Clip.frequency;
            m_MusicSourceIntro.clip = music.Clip;
            // m_MusicSource.PlayDelayed(delay);
            m_MusicSourceIntro.PlayScheduled(time);
            m_MusicSourceIntro.SetScheduledEndTime(time + len);
            m_MusicSourceIntro.loop = false;
            return len;
        }

        return 0;
    }

    public void PlayLoop(string MusicName, double time){
        foreach (var music in m_AudioAsset.Musics.Where(music => music.Name == MusicName)){
            var len = music.Clip.length / music.Clip.frequency;
            m_MusicSourceLoop.clip = music.Clip;
            // m_MusicSource.PlayDelayed(delay);
            m_MusicSourceLoop.PlayScheduled(time);
            
            m_MusicSourceLoop.loop = true;
            return;
        }
    }
    
    public void PlayMusic(string IntroName, string LoopName){
        var t0 = AudioSettings.dspTime + 3.0f;
        var length = PlayIntro(IntroName , t0);
        PlayLoop(LoopName, t0 + length);
    }
    // private IEnumerator StartMethod(float clipLength, string LoopName)
    // {
    //     // yield return new WaitForSecondsRealtime(clipLength);
    //     // PlayMusic(LoopName, true);
    //     // yield return 0;
    // }

    private IEnumerator StartMethod(string LoopName){
        yield return new WaitForEndOfFrame();
        while (m_MusicSourceIntro.isPlaying){
            yield return 0;
        }
        
        // PlayMusic(LoopName, true);
        
        yield return 0;
    }

    
    public void PlaySfx(string SfxName){
        foreach (var music in m_AudioAsset.Sfxs.Where(sfx => sfx.Name == SfxName)){
            var sfxSource = GetEmptySource();
            if (sfxSource == null) return;
            sfxSource.clip = music.Clip;
            sfxSource.Play();
            StartCoroutine(ClearSfxSource(music.Clip.length, sfxSource));
            return;
        }
    }

    private AudioSource GetEmptySource(){
        return m_SfxSource.FirstOrDefault(sfxSource => sfxSource.clip == null);
    }
    
    private IEnumerator ClearSfxSource(float clipLength, AudioSource source)
    {
        yield return new WaitForSeconds(clipLength);
        source.clip = null;
        yield return 0;
    }
    
}
*/
