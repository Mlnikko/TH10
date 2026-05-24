using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.AddressableAssets;



//音频管理器
public class AudioManager : SingletonMono<AudioManager>
{
    const float MinVolumeDb = -40f;
    const float MaxVolumeDb = 0f;

    /// <summary>AudioMixer 暴露的分组音量参数名（须与 Mixer 中 Exposed Parameters 一致）。</summary>
    static class MixerGroupNames
    {
        public const string Master = "Master";
        public const string UI = "UI";
        public const string BGM = "BGM";
        public const string SFX = "SFX";
    }

    [SerializeField] AudioMixer audioMixer;
    [SerializeField] AudioConfig audioConfigs;

    GameObject defaultAudioSourcesRoot;

    Dictionary<AudioName, AudioSource> audiosDict;

    protected override void OnSingletonInit()
    {
        audiosDict = new();

        if (audioConfigs == null) audioConfigs = Addressables.LoadAssetAsync<AudioConfig>("cfg_audioconfig").WaitForCompletion();
        if(audioMixer == null) audioMixer = Addressables.LoadAssetAsync<AudioMixer>("AudioMixer").WaitForCompletion();

        InitAudioManager();

        if (GameSettingsService.Instance != null)
        {
            GameSettingsService.Instance.EnsureLoaded();
            ApplySettings(GameSettingsService.Instance.Data);
        }
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
    public void PlayAudio(AudioName name, bool isWait = false)
    {
        if (name == AudioName.None) return;

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
    public void StopAudio(AudioName name)
    {
        if (name == AudioName.None) return;
        if (!audiosDict.ContainsKey(name))
        {
            Debug.LogWarning("不存在音频" + name);
            return;
        }
        audiosDict[name].Stop();
        //Debug.Log("已暂停播放音频" + name);
    }

    /// <summary>
    /// 淡出停止播放音频
    /// </summary>
    /// <param name="name">音频名称</param>
    /// <param name="fadeDuration">淡出持续时间</param>
    public void StopAudio(AudioName name, float fadeDuration = 1f)
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
    public void PauseAudio(AudioName name)
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

    public void JustPlayOneAudio(AudioName name)
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
    public void UnPauseAudio(AudioName name)
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

    public void ApplySettings(GameSettingsData settings)
    {
        if (settings == null || audioMixer == null) return;

        ApplyChannelVolume(MixerGroupNames.Master, settings.masterVolume, settings.masterMuted);
        ApplyChannelVolume(MixerGroupNames.UI, settings.uiVolume, settings.uiMuted);
        ApplyChannelVolume(MixerGroupNames.BGM, settings.bgmVolume, settings.bgmMuted);
        ApplyChannelVolume(MixerGroupNames.SFX, settings.sfxVolume, settings.sfxMuted);
    }

    void ApplyChannelVolume(string mixerGroupName, float slider0To10, bool muted)
    {
        if (muted)
            SetMixerVolumeDb(mixerGroupName, MinVolumeDb);
        else
            SetMixerVolumeDb(mixerGroupName, VolumeSliderToDb(slider0To10));
    }

    public void SetMasterVolume(float value) => ApplyChannelVolume(MixerGroupNames.Master, value, muted: false);
    public void SetUIVolume(float value) => ApplyChannelVolume(MixerGroupNames.UI, value, muted: false);
    public void SetBGMVolume(float value) => ApplyChannelVolume(MixerGroupNames.BGM, value, muted: false);
    public void SetSFXVolume(float value) => ApplyChannelVolume(MixerGroupNames.SFX, value, muted: false);

    static float VolumeSliderToDb(float slider0To10)
    {
        return Mathf.Lerp(MinVolumeDb, MaxVolumeDb, Mathf.Clamp01(slider0To10 / 10f));
    }

    void SetMixerVolumeDb(string mixerGroupName, float volumeDb)
    {
        if (audioMixer == null || string.IsNullOrEmpty(mixerGroupName)) return;
        if (!audioMixer.SetFloat(mixerGroupName, volumeDb))
            Logger.Warn($"AudioMixer 未找到暴露参数 '{mixerGroupName}'，请在 AudioMixer 中 Expose 对应分组 Volume。", LogTag.Audio);
    }
}
