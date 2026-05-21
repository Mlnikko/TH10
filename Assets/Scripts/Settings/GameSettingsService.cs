using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 设置读写：默认值来自 <see cref="GameSettingsConfig"/> SO（Resources），
/// 玩家覆盖写入 persistentDataPath/user_gamesettings.json（与 SO 字段结构一致）。
/// </summary>
public class GameSettingsService : SingletonMono<GameSettingsService>
{
    const string SaveFileName = "user_gamesettings.json";
    const string DefaultResourcesPath = "GameSettingsConfig";

    [SerializeField]
    [Tooltip("可选：未放在 Resources 时在此指定默认 SO")]
    GameSettingsConfig defaultSettingsAsset;

    GameSettingsData _runtime;
    bool _loaded;

    public GameSettingsData Data
    {
        get
        {
            EnsureLoaded();
            return _runtime;
        }
    }

    public event Action<GameSettingsData> OnSettingsChanged;

    public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    protected override void OnSingletonInit()
    {
        Load();
    }

    public void EnsureLoaded()
    {
        if (!_loaded) Load();
    }

    public void Load()
    {
        var defaults = ResolveDefaultAsset();
        _runtime = defaults != null ? defaults.data.Clone() : GameSettingsData.CreateDefault();

        string path = SaveFilePath;
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                var loaded = JsonUtility.FromJson<GameSettingsData>(json);
                if (loaded != null)
                    _runtime.CopyFrom(loaded);
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to load settings from '{path}': {ex.Message}", LogTag.Config);
            }
        }

        ClampResolutionIndex();
        _loaded = true;
    }

    public void Save()
    {
        EnsureLoaded();
        try
        {
            string json = JsonUtility.ToJson(_runtime, prettyPrint: true);
            File.WriteAllText(SaveFilePath, json);
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to save settings: {ex.Message}", LogTag.Config);
        }
    }

    public void ApplyAll()
    {
        EnsureLoaded();
        GameSettingsApplier.ApplyAll(_runtime);
    }

    public void NotifyChanged(bool save = true, bool apply = true)
    {
        EnsureLoaded();
        if (save) Save();
        if (apply) ApplyAll();
        OnSettingsChanged?.Invoke(_runtime);
    }

    public void ResetToDefaults()
    {
        var defaults = ResolveDefaultAsset();
        _runtime = defaults != null ? defaults.data.Clone() : GameSettingsData.CreateDefault();
        ClampResolutionIndex();
        NotifyChanged();
    }

    GameSettingsConfig ResolveDefaultAsset()
    {
        if (defaultSettingsAsset != null)
            return defaultSettingsAsset;

        return Resources.Load<GameSettingsConfig>(DefaultResourcesPath);
    }

    void ClampResolutionIndex()
    {
        if (_runtime == null) return;
        int max = Mathf.Max(0, DisplayResolutionCatalog.Count - 1);
        _runtime.resolutionPresetIndex = Mathf.Clamp(_runtime.resolutionPresetIndex, 0, max);
        DisplayResolutionCatalog.ApplyPresetToData(_runtime, _runtime.resolutionPresetIndex);
    }
}
