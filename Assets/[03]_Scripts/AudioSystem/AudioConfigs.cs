using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[Serializable]
public class Sounds
{
    [Header("音频名称")]
    public string audioName;

    [Header("音频剪辑")]
    public AudioClip clip;

    [Header("音频分组")]
    public AudioMixerGroup outPutGroup;

    [Header("是否静音")]
    public bool mute;

    [Header("音频音量")]
    [Range(0, 1)]
    public float volume;

    [Header("音频是否开局播放")]
    public bool playOnAwake;

    [Header("音频是否循环播放")]
    public bool loop;
}

[CreateAssetMenu(fileName = "NewAudioConfigs", menuName = "Custom/AudioConfig")]
public class AudioConfigs : ScriptableObject
{
    public List<Sounds> sounds;
}
