using UnityEngine;

/// <summary>将 <see cref="GameSettingsData"/> 应用到运行时系统。</summary>
public static class GameSettingsApplier
{
    public static void ApplyAll(GameSettingsData data)
    {
        if (data == null) return;
        ApplyGraphics(data);
        ApplyAudio(data);
        ApplyControls(data);
    }

    public static void ApplyGraphics(GameSettingsData data)
    {
        if (data == null) return;

        int fps = (int)data.displayRefreshRate;
        if (GameManager.Instance != null)
            GameManager.Instance.SetApplicationFPS(fps, vsync: false);
        else
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = fps;
        }

        var preset = DisplayResolutionCatalog.GetPreset(data.resolutionPresetIndex);
        int w = data.screenWidth > 0 ? data.screenWidth : preset.Width;
        int h = data.screenHeight > 0 ? data.screenHeight : preset.Height;

        var mode = data.windowMode == E_WindowMode.FullScreen
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;

        Screen.SetResolution(w, h, mode);
    }

    public static void ApplyAudio(GameSettingsData data)
    {
        if (data == null || AudioManager.Instance == null) return;
        AudioManager.Instance.ApplySettings(data);
    }

    public static void ApplyControls(GameSettingsData data)
    {
        if (data == null || InputManager.Instance == null) return;
        InputManager.Instance.ApplyKeyConfig(data.keyBindings);
    }
}
