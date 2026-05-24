using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 战斗场景直连 Play 时的 StageTimeline 预览运行时：异步初始化 GameResDB / 对象池 / GlobalBattleData。
/// 不依赖 <see cref="GameLauncher"/> 或正式进战流程。
/// </summary>
public static class StageTimelinePreviewRuntime
{
    enum LoadState
    {
        Idle,
        Loading,
        Ready,
        Failed
    }

    static LoadState _state = LoadState.Idle;
    static string _lastError;
    static Task _loadTask;

    public const string PlayModeRequiredMessage = "请先点击 Unity 编辑器上方的「运行」，进入 Play 模式后再预览。";

    public static bool IsReady => _state == LoadState.Ready;
    public static bool IsLoading => _state == LoadState.Loading;
    public static string LastError => _lastError;

    public static bool IsPlayModeReady =>
        Application.isPlaying && IsReady;

    public static Task EnsureReadyAsync(BattleAreaConfig battleAreaConfig = null)
    {
        if (!Application.isPlaying)
            return Task.FromException(new InvalidOperationException(PlayModeRequiredMessage));

        if (_state == LoadState.Ready)
            return Task.CompletedTask;

        if (_state == LoadState.Loading && _loadTask != null)
            return _loadTask;

        _state = LoadState.Loading;
        _lastError = null;
        _loadTask = LoadInternalAsync(battleAreaConfig);
        return _loadTask;
    }

    static async Task LoadInternalAsync(BattleAreaConfig battleAreaConfig)
    {
        try
        {
            _ = GameManager.Instance;

            if (!GameResDB.IsInitialized)
            {
                await ResManager.Instance.InitializeAsync().ConfigureAwait(true);
                await GameResDB.Instance.InitializeAsync().ConfigureAwait(true);
            }

            WarmupObjectPools();
            TryApplyGlobalBattleData(battleAreaConfig);

            _state = LoadState.Ready;
            Logger.Info("[StageTimelinePreview] 预览运行时就绪（GameResDB + 对象池）。", LogTag.Config);
        }
        catch (Exception ex)
        {
            _state = LoadState.Failed;
            _lastError = ex.Message;
            Logger.Error($"[StageTimelinePreview] 初始化失败: {ex.Message}", LogTag.Config);
            throw;
        }
    }

    static void WarmupObjectPools()
    {
        _ = GameObjectPoolManager.Instance;

        var globalPoolConfig = GameResDB.Instance.GetConfig<GlobalPoolConfig>("defaultglobalpool");
        if (globalPoolConfig == null)
            throw new InvalidOperationException("未找到 GlobalPoolConfig 'defaultglobalpool'。");

        int maxPrefabIndex = GameResDB.Instance.GetMaxPrefabIndex();
        if (maxPrefabIndex < 0)
            throw new InvalidOperationException("GameResDB 中无预制体索引。");

        GameObjectPoolManager.Instance.Initialize(maxPrefabIndex);

        for (int i = 0; i < globalPoolConfig.poolCategories.Length; i++)
        {
            var categoryGroup = globalPoolConfig.poolCategories[i];
            for (int j = 0; j < categoryGroup.entries.Length; j++)
            {
                var entry = categoryGroup.entries[j];
                GameObjectPoolManager.Instance.WarmupPool(entry.prefabId, entry.defaultWarmupCount);
            }
        }
    }

    static void TryApplyGlobalBattleData(BattleAreaConfig battleAreaConfig)
    {
        if (GlobalBattleData.IsInitialized)
            return;

        var resolved = ResolveBattleAreaConfig(battleAreaConfig);
        if (resolved != null)
            GlobalBattleData.Initialize(resolved);
    }

    public static BattleAreaConfig ResolveBattleAreaConfig(BattleAreaConfig explicitConfig)
    {
        if (explicitConfig != null)
            return explicitConfig;

        if (!GameResDB.IsInitialized)
            return null;

        var manifest = ResManager.Instance?.Manifest;
        if (manifest == null || string.IsNullOrEmpty(manifest.battleAreaConfigId))
            return null;

        return GameResDB.Instance.GetConfig<BattleAreaConfig>(manifest.battleAreaConfigId);
    }

    public static bool TryValidateForPreview(BattleAreaConfig battleAreaConfig, out string error)
    {
        if (!Application.isPlaying)
        {
            error = PlayModeRequiredMessage;
            return false;
        }

        if (_state == LoadState.Loading)
        {
            error = "预览资源加载中，请稍候…";
            return false;
        }

        if (_state == LoadState.Failed)
        {
            error = string.IsNullOrEmpty(_lastError)
                ? "预览运行时初始化失败。"
                : _lastError;
            return false;
        }

        if (_state != LoadState.Ready || !GameResDB.IsInitialized)
        {
            error = "预览运行时未就绪。";
            return false;
        }

        if (ResolveBattleAreaConfig(battleAreaConfig) == null)
        {
            error = "未指定 BattleAreaConfig，且无法从 Manifest 解析战斗区。";
            return false;
        }

        error = null;
        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void RegisterPlayModeReset()
    {
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            ResetSession();
    }
#endif

    public static void ResetSession()
    {
        _state = LoadState.Idle;
        _lastError = null;
        _loadTask = null;
    }
}
