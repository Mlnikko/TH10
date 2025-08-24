using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

//音频管理器
public class AudioManager : SingletonMono<AudioManager>
{
    const float minVolume = -40f;
    const float maxVolume = 0.0f;

    [SerializeField] AudioMixer audioMixer;
    [SerializeField] AudioConfig audioConfigs;

    UnityEngine.GameObject defaultAudioSourcesRoot;

    Dictionary<E_AudioName, AudioSource> audiosDict;

    protected override void OnSingletonInit()
    {
        audiosDict = new();
        InitAudioManager();
    }  

    void Start()
    {
        //ApplicationSoundsSetting();
    }

    /// <summary>
    /// 播放某一个音频
    /// </summary>
    /// <param name="name">音频名称</param>
    /// <param name="isWait">是否强制重新播放</param>
    public void PlayAudio(E_AudioName name, bool isWait = false)
    {
        if (name == E_AudioName.NULL) return;

        if (!audiosDict.ContainsKey(name))
        {
            Debug.LogWarning("名为" + name + "的音频不存在");
            return;
        }
        if (isWait)
        {
            if (!audiosDict[name].isPlaying)
            {
                audiosDict[name].Play();
            }
        }
        else
        {
            audiosDict[name].Play();
            //Log.Log("已开始播放音频" + name);
        }
    }

    /// <summary>
    /// 立即停止播放音频
    /// </summary>
    /// <param name="name">音频名称</param>
    public void StopAudio(E_AudioName name)
    {
        if (name == E_AudioName.NULL) return;
        if (!audiosDict.ContainsKey(name))
        {
            Debug.LogWarning("不存在音频" + name);
            return;
        }
        audiosDict[name].Stop();
        Debug.Log("已暂停播放音频" + name);
    }

    /// <summary>
    /// 淡出停止播放音频
    /// </summary>
    /// <param name="name">音频名称</param>
    /// <param name="fadeDuration">淡出持续时间</param>
    public void StopAudio(E_AudioName name, float fadeDuration = 1f)
    {
        if (!audiosDict.ContainsKey(name))
        {
            Debug.LogWarning("不存在音频" + name);
            return;
        }
        Instance.StartCoroutine(Instance.AudioFadeOutAndStopCoroutine(audiosDict[name], fadeDuration));
    }

    /// <summary>
    /// 暂停播放音频
    /// </summary>
    /// <param name="name">音频名称</param>
    public void PauseAudio(E_AudioName name)
    {
        if (!audiosDict.ContainsKey(name))
        {
            Debug.LogWarning("不存在音频" + name);
            return;
        }
        else if (!audiosDict[name].isPlaying)
        {
            Debug.LogWarning("音频未开始播放，不可暂停" + name);
        }
        else
        {
            audiosDict[name].Pause();
            Debug.Log("已暂停播放音频" + name);
        }
    }

    public void JustPlayOneAudio(E_AudioName name)
    {
        StopAllAudio();
        PlayAudio(name);
    }

    public void StopAllAudio()
    {
        foreach (AudioSource audioSource in audiosDict.Values)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// 继续播放音频
    /// </summary>
    /// <param name="name">音频名称</param>
    public void UnPauseAudio(E_AudioName name)
    {
        if (!audiosDict.ContainsKey(name))
        {
            Debug.LogWarning("不存在音频" + name);
            return;
        }
        else if (audiosDict[name].isPlaying)
        {
            Debug.LogWarning("音频正在播放，不可继续播放" + name);
        }
        else
        {
            audiosDict[name].UnPause();
            //Log.Log("已继续播放音频" + name);
        }
    }

    /// <summary>
    /// 初始化
    /// </summary>
    void InitAudioManager()
    {
        if (defaultAudioSourcesRoot == null)
        {
            defaultAudioSourcesRoot = new UnityEngine.GameObject("AllAudio");
            defaultAudioSourcesRoot.transform.SetParent(transform);
        }
        foreach(var audioGroup in audioConfigs.audioGroups)
        {
            foreach (var audioData in audioGroup.audioDatas)
            {
                AudioSource source = defaultAudioSourcesRoot.AddComponent<AudioSource>();
                source.clip = audioData.clip;
                source.playOnAwake = audioData.isPlayOnAwake;
                source.mute = audioData.isMute;
                source.loop = audioData.isLoop;
                source.volume = audioData.volume;
                source.outputAudioMixerGroup = audioData.outPutGroup;

                if (audioData.isPlayOnAwake)
                {
                    source.Play();
                    Debug.Log("已唤醒播放音频" + audioData.audioName);
                }

                audiosDict.Add(audioData.audioName, source);
            }
        }     
    }

    //void ApplicationSoundsSetting()
    //{
    //    float masterValue = GameSettingManager.Instance.GetGameSettingData().masterVolume;
    //    float bgmValue = GameSettingManager.Instance.GetGameSettingData().bgmVolume;
    //    float sfxValue = GameSettingManager.Instance.GetGameSettingData().sfxVolume;
    //    SetMasterVolume(masterValue);
    //    SetBGMVolume(bgmValue);
    //    SetSFXVolume(sfxValue);
    //}

    IEnumerator AudioFadeOutAndStopCoroutine(AudioSource audioSource, float fadeDuration)
    {
        float startVolume = audioSource.volume;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }

        audioSource.Stop();
        audioSource.volume = startVolume;
    }

    public void SetMasterVolume(float value)
    {
        float volume = Mathf.Lerp(minVolume, maxVolume, value / 10);
        audioMixer.SetFloat("MasterVolume", volume); // 确保参数名称与音频混合器中的相匹配
    }

    public void SetBGMVolume(float value)
    {
        float volume = Mathf.Lerp(minVolume, maxVolume, value / 10);
        audioMixer.SetFloat("BGMVolume", volume); // 确保参数名称与音频混合器中的相匹配
    }

    public void SetSFXVolume(float value)
    {
        float volume = Mathf.Lerp(minVolume, maxVolume, value / 10);
        audioMixer.SetFloat("SFXVolume", volume); // 确保参数名称与音频混合器中的相匹配
    }
}
