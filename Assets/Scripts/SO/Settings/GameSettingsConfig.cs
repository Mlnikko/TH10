using System;
using UnityEngine;

/// <summary>显示刷新率（渲染帧率上限）。</summary>
public enum E_DisplayRefreshRate
{
    Hz60 = 60,
    Hz120 = 120,
}

/// <summary>窗口模式。</summary>
public enum E_WindowMode
{
    FullScreen = 0,
    Windowed = 1,
}

/// <summary>
/// 游戏设置数据（与 <see cref="GameSettingsConfig"/> 字段一致，可 JSON 持久化到本地）。
/// </summary>
[Serializable]
public class GameSettingsData
{
    public E_DisplayRefreshRate displayRefreshRate = E_DisplayRefreshRate.Hz60;
    public E_WindowMode windowMode = E_WindowMode.FullScreen;
    public int resolutionPresetIndex;
    public int screenWidth = 1920;
    public int screenHeight = 1080;

    [Range(0f, 10f)] public float masterVolume = 10f;
    public bool masterMuted;
    [Range(0f, 10f)] public float uiVolume = 10f;
    public bool uiMuted;
    [Range(0f, 10f)] public float bgmVolume = 10f;
    public bool bgmMuted;
    [Range(0f, 10f)] public float sfxVolume = 10f;
    public bool sfxMuted;

    public InputKeyCodeConfig keyBindings = new();

    public static GameSettingsData CreateDefault()
    {
        return new GameSettingsData();
    }

    public GameSettingsData Clone()
    {
        return JsonUtility.FromJson<GameSettingsData>(JsonUtility.ToJson(this));
    }

    public void CopyFrom(GameSettingsData other)
    {
        if (other == null) return;
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(other), this);
    }
}

/// <summary>
/// 默认设置 ScriptableObject（放在 Resources 或 Configs 中作为出厂默认值）。
/// 玩家修改后由 <see cref="GameSettingsService"/> 写入 persistentDataPath 下的 JSON。
/// </summary>
[CreateAssetMenu(fileName = "GameSettingsConfig", menuName = "Configs/GameSettingsConfig")]
public class GameSettingsConfig : ScriptableObject
{
    public GameSettingsData data = GameSettingsData.CreateDefault();
}
