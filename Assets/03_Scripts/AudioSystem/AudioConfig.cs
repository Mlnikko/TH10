using System;
using UnityEngine;
using UnityEngine.Audio;

public enum E_AudioName
{
    NULL,
    // SFX
    Cancel,
    Confirm,
    Select,
    Pause,

    Danmaku_Shoot,

    // BGM
    Title,
    Stage1_Start,
}

public enum E_AudioGroup
{
    BGM,
    SFX
}

[Serializable]
public class AudioGroup
{
    [SerializeField] 
    string displayName;

    [Header("“Ù∆µ∑÷¿‡")]
    public E_AudioGroup audioGroup;
    [Header("“Ù∆µ≈‰÷√±Ì")]
    public AudioData[] audioDatas;
}

[Serializable]
public class AudioData
{
    [SerializeField]
    string displayName;

    [Header("“Ù∆µ√˚≥∆")]
    public E_AudioName audioName;

    [Header("“Ù∆µºÙº≠")]
    public AudioClip clip;

    [Header("“Ù∆µ∑÷◊È")]
    public AudioMixerGroup outPutGroup;

    [Header(" «∑Òæ≤“Ù")]
    public bool isMute;

    [Header("“Ù∆µ“Ù¡ø")]
    [Range(0, 1)]
    public float volume;

    [Header("“Ù∆µ «∑Òø™æ÷≤•∑≈")]
    public bool isPlayOnAwake;

    [Header("“Ù∆µ «∑Ò—≠ª∑≤•∑≈")]
    public bool isLoop;
}

[CreateAssetMenu(fileName = "NewAudioConfig", menuName = "Custom/AudioConfig")]
public class AudioConfig : ScriptableObject
{
    public AudioGroup[] audioGroups;
}
