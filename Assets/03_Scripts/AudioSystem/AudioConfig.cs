using System;
using UnityEngine;
using UnityEngine.Audio;

public enum AudioName
{
    None = 0,
    // SFX
    Cancel = 1,
    Confirm = 2,
    Select = 3,
    Pause = 4,

    Danmaku_Shoot,


    Player_Die,
    Enemy_Die_0,
    Enemy_Die_1,

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
    public AudioName audioName;

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
