#if UNITY_EDITOR
using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// StageTimeline 编辑器预览辅助：仅在 Inspector 手动点击时加载资源，不参与运行时逻辑。
/// </summary>
public static class StageTimelinePreviewRuntime
{
    static bool _loading;
    static string _lastError;
    static bool _previewOwnedGlobalBattleData;

    public const string PlayModeRequiredMessage = "请先进入 Play 模式后再预览。";

    public const string InBattleBlockedMessage = "战斗进行中已禁用预览，避免影响正常游戏流程。";

    public static bool IsLoading => _loading;
    public static string LastError => _lastError;

    public static bool CanPreview =>
        Application.isPlaying && !IsInActiveBattle;

    static bool IsInActiveBattle =>
        BattleManager.Instance != null
        && BattleManager.Instance.CurrentStatus == E_BattleStatus.InBattle;

    public static Task EnsureReadyAsync(BattleAreaConfig battleAreaConfig)
    {
        if (!Application.isPlaying)
            return Task.FromException(new InvalidOperationException(PlayModeRequiredMessage));

        if (IsInActiveBattle)
            return Task.FromException(new InvalidOperationException(InBattleBlockedMessage));

        if (GameResDB.IsInitialized)
            return Task.CompletedTask;

        return LoadGameResDbAsync();
    }

    static async Task LoadGameResDbAsync()
    {
        if (_loading)
            return;

        _loading = true;
        _lastError = null;
        try
        {
            _ = GameManager.Instance;
            await ResManager.Instance.InitializeAsync().ConfigureAwait(true);
            await GameResDB.Instance.InitializeAsync().ConfigureAwait(true);
            WarmupObjectPoolsIfNeeded();
        }
        catch (Exception ex)
        {
            _lastError = ex.Message;
            throw;
        }
        finally
        {
            _loading = false;
        }
    }

    static void WarmupObjectPoolsIfNeeded()
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

    public static bool TryApplyPreviewBattleArea(BattleAreaConfig battleAreaConfig, out string error)
    {
        error = null;
        var resolved = ResolveBattleAreaConfig(battleAreaConfig);
        if (resolved == null)
        {
            error = "未指定 BattleAreaConfig，且无法从 Manifest 解析战斗区。";
            return false;
        }

        if (GlobalBattleData.IsInitialized)
            return true;

        GlobalBattleData.Initialize(resolved);
        _previewOwnedGlobalBattleData = true;
        return true;
    }

    public static void ReleasePreviewBattleAreaIfOwned()
    {
        if (!_previewOwnedGlobalBattleData)
            return;

        if (GlobalBattleData.IsInitialized)
            GlobalBattleData.ResetForEditorPreview();

        _previewOwnedGlobalBattleData = false;
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

        if (IsInActiveBattle)
        {
            error = InBattleBlockedMessage;
            return false;
        }

        if (!GameResDB.IsInitialized)
        {
            error = "GameResDB 未就绪。";
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

    public static void ResetSession()
    {
        _loading = false;
        _lastError = null;
        _previewOwnedGlobalBattleData = false;
    }

    [UnityEditor.InitializeOnLoadMethod]
    static void RegisterPlayModeReset()
    {
        UnityEditor.EditorApplication.playModeStateChanged += state =>
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                ResetSession();
        };
    }
}
#endif
