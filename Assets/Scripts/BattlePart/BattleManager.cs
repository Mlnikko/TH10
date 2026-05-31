using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public struct PlayerBattleData
{
    public byte playerIndex;
    public E_Character characterId;
    public E_Weapon weaponId;

    public PlayerBattleData(byte index, E_Character character, E_Weapon weapon)
    {
        playerIndex = index;
        characterId = character;
        weaponId = weapon;
    }
}

public enum E_BattleStatus
{
    Prepare,
    InBattle
}

/// <summary>战斗暂停原因（影响暂停菜单按钮与是否允许恢复）。</summary>
public enum E_BattlePauseReason
{
    None,
    /// <summary>玩家手动暂停，可继续。</summary>
    Manual,
    /// <summary>单机生命归零，仅可重新开始。</summary>
    GameOverSingle,
    /// <summary>联机全员生命归零，仅可返回房间。</summary>
    GameOverMulti,
    /// <summary>单机关卡通关，可重新开始或退出。</summary>
    StageClearSingle,
    /// <summary>联机关卡通关；房主可重新开始，全员可返回房间。</summary>
    StageClearMulti,
}

/// <summary>Boss 血条 HUD 只读快照（供 <see cref="BattleUIPanel"/> 轮询）。</summary>
public readonly struct BossHudSnapshot
{
    public readonly int CurrentHealth;
    public readonly int MaxHealth;
    public readonly int BossPhaseIndex;
    public readonly string DisplayName;
    /// <summary>Boss 在战斗区内的水平归一化位置 [0=左缘, 1=右缘]。</summary>
    public readonly float NormalizedHorizontal;

    public float NormalizedHealth =>
        MaxHealth > 0 ? Mathf.Clamp01((float)CurrentHealth / MaxHealth) : 0f;

    public BossHudSnapshot(
        int currentHealth,
        int maxHealth,
        int bossPhaseIndex,
        string displayName,
        float normalizedHorizontal)
    {
        CurrentHealth = currentHealth;
        MaxHealth = maxHealth;
        BossPhaseIndex = bossPhaseIndex;
        DisplayName = displayName ?? string.Empty;
        NormalizedHorizontal = Mathf.Clamp01(normalizedHorizontal);
    }

    public static float WorldXToNormalizedHorizontal(float worldX, in BattleAreaData area)
    {
        if (area.Width <= 0.001f)
            return 0.5f;
        return Mathf.Clamp01((worldX - area.Left) / area.Width);
    }
}

/// <summary>战斗 HUD 只读快照（供 UI 轮询）。</summary>
public readonly struct BattleHudSnapshot
{
    public readonly int Score;
    public readonly int HealthCurrent;
    public readonly int HealthMax;
    public readonly int PowerOrbs;

    public BattleHudSnapshot(int score, int healthCurrent, int healthMax, int powerOrbs)
    {
        Score = score;
        HealthCurrent = healthCurrent;
        HealthMax = healthMax;
        PowerOrbs = powerOrbs;
    }
}

/// <summary>战斗运行时调试快照（供 HUD RuntimeData 显示）。</summary>
public readonly struct BattleRuntimeSnapshot
{
    public readonly float RenderFps;
    public readonly float LogicFps;
    public readonly int ActiveEntityCount;
    public readonly int ActiveGameObjectCount;

    public BattleRuntimeSnapshot(float renderFps, float logicFps, int activeEntityCount, int activeGameObjectCount)
    {
        RenderFps = renderFps;
        LogicFps = logicFps;
        ActiveEntityCount = activeEntityCount;
        ActiveGameObjectCount = activeGameObjectCount;
    }
}

public static class GlobalBattleData
{
    public static BattleAreaData AreaData { get; private set; }
    public static PlayerSpawnData SpawnData { get; private set; }
    public static DropItemCollectData DropItemCollectData { get; private set; }
    public static BattleAreaBackgroundData BackgroundData { get; private set; } = new();

    public static bool IsInitialized { get; private set; }

    /// <summary>当前战斗会话得分（切关/重开战斗时由 <see cref="BattleManager.BeginBattleSession"/> 重置）。</summary>
    public static int SessionScore { get; private set; }

    public static void Initialize(BattleAreaConfig config)
    {
        AreaData = config.battleAreaData;
        SpawnData = config.playerSpawnData;
        DropItemCollectData = config.dropItemCollectData;
        IsInitialized = true;
    }

    /// <summary>开战时由关卡时间轴写入背景配置。</summary>
    public static void ApplyStageTimeline(StageTimelineConfig timeline)
    {
        BackgroundData = timeline?.backgroundData ?? new BattleAreaBackgroundData();
    }

    public static void ResetBattleSessionStats()
    {
        SessionScore = 0;
    }

    public static void AddSessionScore(int delta)
    {
        if (delta <= 0) return;
        SessionScore += delta;
    }

#if UNITY_EDITOR
    /// <summary>编辑器 ConfigViewer 预览结束后还原战斗区全局状态。</summary>
    public static void ResetForEditorPreview()
    {
        AreaData = default;
        SpawnData = default;
        DropItemCollectData = default;
        BackgroundData = new BattleAreaBackgroundData();
        IsInitialized = false;
    }
#endif
}

/// <summary>
/// 战斗会话入口：进场景 + 准备 UI + ECS 开战均由此类编排。
/// </summary>
[DefaultExecutionOrder(100)]
public class BattleManager : SingletonMono<BattleManager>
{
    public const string BattleSceneName = "BattleScene";

    public bool isSinglePlayerMode;

    public E_BattleStatus CurrentStatus { get; private set; } = E_BattleStatus.Prepare;
    public List<PlayerBattleData> allPlayerDatas = new(4);

    readonly bool[] _activePlayers = new bool[4];

    int TotalPlayers => allPlayerDatas.Count;

    /// <summary>当前战斗会话中的玩家数量（供 ECS 复活等逻辑使用）。</summary>
    public int TotalPlayerCount => TotalPlayers;

    public bool TryGetPlayerBattleData(byte playerIndex, out PlayerBattleData data)
    {
        for (int i = 0; i < allPlayerDatas.Count; i++)
        {
            if (allPlayerDatas[i].playerIndex != playerIndex)
                continue;

            data = allPlayerDatas[i];
            return true;
        }

        data = default;
        return false;
    }

    World _battleWorld;

    public World ActiveBattleWorld => _battleWorld;

    /// <summary>战斗逻辑帧是否已暂停。</summary>
    public bool IsBattlePaused { get; private set; }

    /// <summary>当前暂停原因。</summary>
    public E_BattlePauseReason PauseReason => _pauseReason;

    /// <summary>因生命归零触发的强制暂停（不可继续游戏）。</summary>
    public bool IsGameOverPaused =>
        IsBattlePaused
        && (_pauseReason == E_BattlePauseReason.GameOverSingle
            || _pauseReason == E_BattlePauseReason.GameOverMulti);

    /// <summary>关卡通关后的强制暂停（不可继续游戏）。</summary>
    public bool IsStageClearPaused =>
        IsBattlePaused
        && (_pauseReason == E_BattlePauseReason.StageClearSingle
            || _pauseReason == E_BattlePauseReason.StageClearMulti);

    /// <summary>Game Over 或关卡通关等不可恢复的终局暂停。</summary>
    public bool IsTerminalBattlePaused => IsGameOverPaused || IsStageClearPaused;

    /// <summary>联机关卡通关后，房主可发起重新开始。</summary>
    public bool CanHostRestartAfterStageClear =>
        IsStageClearPaused && !isSinglePlayerMode && IsLocalNetworkHost();

    /// <summary>手动暂停且可恢复（联机仅房主）。</summary>
    public bool CanResumeBattle =>
        IsBattlePaused
        && _pauseReason == E_BattlePauseReason.Manual
        && (isSinglePlayerMode || IsLocalNetworkHost());

    /// <summary>联机中本地玩家已生命归零、战斗仍在进行（观战）。</summary>
    public bool IsLocalSpectating =>
        !isSinglePlayerMode
        && CurrentStatus == E_BattleStatus.InBattle
        && IsPlayerEliminated(RoomManager.LocalPlayerIndex)
        && !IsTerminalBattlePaused;

    /// <summary>按暂停键可暂停（单机任意玩家；联机仅房主）。</summary>
    public bool CanPauseBattle =>
        CurrentStatus == E_BattleStatus.InBattle
        && _battleWorld != null
        && !IsTerminalBattlePaused
        && !IsLocalSpectating
        && (isSinglePlayerMode || IsLocalNetworkHost());

    E_BattlePauseReason _pauseReason = E_BattlePauseReason.None;
    byte _eliminatedPlayerMask;
    bool _stageClearHandled;

    readonly BattlePrepareCharacterRegistry _prepareCharacterRegistry = new();

    /// <summary>某玩家确认准备并锁定角色后（playerIndex, characterId）。</summary>
    public event Action<byte, E_Character> OnPrepareCharacterLocked;

    /// <summary>某玩家取消准备并释放角色锁定。</summary>
    public event Action<byte> OnPrepareCharacterReleased;

    [Header("关卡时间线")]
    [Tooltip("StageTimelineConfig 的 ConfigId（SO 文件名小写）；运行时从 GameResDB 读取")]
    [SerializeField] string _activeStageTimelineConfigId = "stagetimeline_1";

    public string ActiveStageTimelineConfigId => _activeStageTimelineConfigId;

    #region 进战斗场景（加载 + 准备面板）

    /// <summary>
    /// 统一入口：关闭 UI → 加载 BattleScene → 重置准备态 → 同步战斗区 → 打开准备面板。
    /// 单机菜单与联机 <see cref="RoomManager.HandleEnterBattleScene"/> 均调用此方法。
    /// </summary>
    public async Task<bool> LoadBattleSceneAndShowPrepareAsync()
    {
        UIManager.Instance.CloseAll();

        if (!await SceneLoader.LoadSceneAsync(BattleSceneName))
        {
            Logger.Error("[Battle] Failed to load BattleScene.", LogTag.Battle);
            return false;
        }

        return await ShowBattlePrepareAsync();
    }

    /// <summary>已在 BattleScene 时仅打开准备面板（一般请用 <see cref="LoadBattleSceneAndShowPrepareAsync"/>）。</summary>
    public async Task<bool> ShowBattlePrepareAsync()
    {
        ResetPrepareSession();
        EnsureGlobalBattleDataReady();

        if (!GlobalBattleData.IsInitialized)
        {
            Logger.Critical("[Battle] GlobalBattleData not ready; check GameResourceManifest.battleAreaConfigId.", LogTag.Battle);
            return false;
        }

        await UIManager.Instance.ShowPanelAsync<BattlePreparePanel>();
        return true;
    }

    /// <summary>新开一局准备阶段：清空玩家列表、释放旧 ECS 世界。</summary>
    void ResetPrepareSession()
    {
        allPlayerDatas.Clear();
        Array.Clear(_activePlayers, 0, _activePlayers.Length);
        _prepareCharacterRegistry.Reset();
        CurrentStatus = E_BattleStatus.Prepare;
        EndBattleSession();
    }

    /// <summary>释放战斗 ECS、表现桥接与对象池，供退出战斗或重新进战前调用。</summary>
    public void EndBattleSession()
    {
        IsBattlePaused = false;
        _pauseReason = E_BattlePauseReason.None;
        _eliminatedPlayerMask = 0;
        _stageClearHandled = false;

        if (_battleWorld != null)
        {
            _battleWorld.GetSystem<StageTimelineSystem>()?.End();
            _battleWorld.Dispose();
            _battleWorld = null;
        }

        PresentationRuntime.Reset();

        InputManager.Instance?.ClearAllInputs();

        // 背景云雾等非 ECS 借出的池对象须在 ShutdownBattlePools 之前归还。
        BattleStageBackgroundPresenter.Release();

        if (GameObjectPoolManager.Instance != null)
            GameObjectPoolManager.Instance.ShutdownBattlePools();

        BattleRuntimeMetrics.Reset();
    }

    public void SetBattlePaused(bool paused)
    {
        if (CurrentStatus != E_BattleStatus.InBattle || _battleWorld == null)
            return;

        if (!paused && IsTerminalBattlePaused)
            return;

        if (IsBattlePaused == paused)
            return;

        IsBattlePaused = paused;
        if (paused)
        {
            if (_pauseReason == E_BattlePauseReason.None)
                _pauseReason = E_BattlePauseReason.Manual;
            _battleWorld.LogicFrameTimer.Pause();
        }
        else
        {
            _pauseReason = E_BattlePauseReason.None;
            _battleWorld.LogicFrameTimer.Resume();
        }
    }

    /// <summary>生命归零后的强制暂停（不可恢复）。</summary>
    public void ForceGameOverPause(E_BattlePauseReason reason)
    {
        if (CurrentStatus != E_BattleStatus.InBattle || _battleWorld == null)
            return;

        if (reason != E_BattlePauseReason.GameOverSingle
            && reason != E_BattlePauseReason.GameOverMulti)
        {
            return;
        }

        if (IsBattlePaused && _pauseReason == reason)
            return;

        if (reason == E_BattlePauseReason.GameOverMulti)
            _eliminatedPlayerMask = BuildActivePlayerMask();

        _pauseReason = reason;
        IsBattlePaused = true;
        _battleWorld.LogicFrameTimer.Pause();
        Logger.Info($"[Battle] Game over pause ({reason}).", LogTag.Battle);
    }

    /// <summary>关卡通关后的强制暂停（由 <see cref="StageTimelineSystem"/> 在 Boss 击败或时间轴结束时调用）。</summary>
    public void NotifyStageCleared()
    {
        if (_stageClearHandled || CurrentStatus != E_BattleStatus.InBattle || _battleWorld == null)
            return;

        _stageClearHandled = true;

        if (isSinglePlayerMode)
        {
            ForceStageClearPause(E_BattlePauseReason.StageClearSingle);
            return;
        }

        var net = NetworkManager.Instance;
        if (net != null && net.NetworkRole == NetworkRole.Host)
            net.Broadcast(new BattleStageClearMSG());

        ForceStageClearPause(E_BattlePauseReason.StageClearMulti);
    }

    public void ClientApplyBattleStageClear()
    {
        if (_stageClearHandled)
            return;

        _stageClearHandled = true;
        ForceStageClearPause(E_BattlePauseReason.StageClearMulti);
    }

    void ForceStageClearPause(E_BattlePauseReason reason)
    {
        if (CurrentStatus != E_BattleStatus.InBattle || _battleWorld == null)
            return;

        if (reason != E_BattlePauseReason.StageClearSingle
            && reason != E_BattlePauseReason.StageClearMulti)
        {
            return;
        }

        if (IsBattlePaused && _pauseReason == reason)
            return;

        _pauseReason = reason;
        IsBattlePaused = true;
        _battleWorld.LogicFrameTimer.Pause();
        Logger.Info($"[Battle] Stage clear pause ({reason}).", LogTag.Battle);
    }

    public bool IsPlayerEliminated(byte playerIndex)
    {
        if (playerIndex >= 8)
            return false;
        return (_eliminatedPlayerMask & (1 << playerIndex)) != 0;
    }

    /// <summary>玩家生命归零时由 <see cref="PlayerHitHandler"/> 调用。</summary>
    public void NotifyPlayerEliminated(byte playerIndex)
    {
        if (_stageClearHandled || IsTerminalBattlePaused)
            return;

        if (!IsPlayerIndexActive(playerIndex))
            return;

        byte bit = (byte)(1 << playerIndex);
        if ((_eliminatedPlayerMask & bit) != 0)
            return;

        _eliminatedPlayerMask |= bit;
        Logger.Info($"[Battle] Player {playerIndex} eliminated.", LogTag.Battle);

        if (isSinglePlayerMode)
        {
            ForceGameOverPause(E_BattlePauseReason.GameOverSingle);
            return;
        }

        if (playerIndex == RoomManager.LocalPlayerIndex)
            Logger.Info("[Battle] Local player eliminated; spectating.", LogTag.Battle);

        TryTriggerMultiplayerGameOver();
    }

    void TryTriggerMultiplayerGameOver()
    {
        byte activeMask = BuildActivePlayerMask();
        if (activeMask == 0 || (_eliminatedPlayerMask & activeMask) != activeMask)
            return;

        var net = NetworkManager.Instance;
        if (net != null && net.NetworkRole == NetworkRole.Host)
            net.Broadcast(new BattleGameOverMSG());

        ForceGameOverPause(E_BattlePauseReason.GameOverMulti);
    }

    public void ClientApplyBattleGameOver()
    {
        ForceGameOverPause(E_BattlePauseReason.GameOverMulti);
    }

    /// <summary>单机 Game Over / 关卡通关后重新开始当前关卡。</summary>
    public void RestartSinglePlayerBattle()
    {
        if (!isSinglePlayerMode || allPlayerDatas.Count == 0)
            return;

        if (_pauseReason != E_BattlePauseReason.GameOverSingle
            && _pauseReason != E_BattlePauseReason.StageClearSingle)
        {
            return;
        }

        ResetBattleSessionForRestart();
        BeginBattleSession(singlePlayer: true, logicStartFrame: 0, remotePlayerDatas: null);
    }

    /// <summary>联机关卡通关后，房主重新开始本关。</summary>
    public void HostRequestRestartMultiplayerBattle()
    {
        if (!CanHostRestartAfterStageClear || allPlayerDatas.Count == 0)
            return;

        var playerDatas = allPlayerDatas.ToArray();
        NetworkManager.Instance?.Broadcast(new BattleRestartMSG
        {
            startFrame = 0,
            randomSeed = 0,
            playerDatas = playerDatas
        });

        RestartMultiplayerBattle(playerDatas);
    }

    public void ClientApplyBattleRestart(PlayerBattleData[] playerDatas)
    {
        if (playerDatas == null || playerDatas.Length == 0)
            return;

        RestartMultiplayerBattle(playerDatas);
    }

    void RestartMultiplayerBattle(PlayerBattleData[] playerDatas)
    {
        if (isSinglePlayerMode)
            return;

        ResetBattleSessionForRestart();
        BeginBattleSession(singlePlayer: false, logicStartFrame: 0, remotePlayerDatas: playerDatas);
    }

    void ResetBattleSessionForRestart()
    {
        IsBattlePaused = false;
        _pauseReason = E_BattlePauseReason.None;
        _eliminatedPlayerMask = 0;
        _stageClearHandled = false;
    }

    #region 联机暂停（仅房主）

    static bool IsLocalNetworkHost()
    {
        var net = NetworkManager.Instance;
        return net != null && net.NetworkRole == NetworkRole.Host;
    }

    /// <summary>本地按暂停键：单机立即暂停；联机仅房主暂停并广播。</summary>
    public void LocalRequestPause()
    {
        if (!CanPauseBattle || IsBattlePaused)
            return;

        if (isSinglePlayerMode)
        {
            SetBattlePaused(true);
            return;
        }

        HostPauseAndBroadcast();
    }

    /// <summary>恢复战斗：单机本地恢复；联机仅房主广播恢复。</summary>
    public void LocalRequestResumeBattle()
    {
        if (!CanResumeBattle)
            return;

        if (isSinglePlayerMode)
        {
            SetBattlePaused(false);
            return;
        }

        HostResumeAndBroadcast();
    }

    void HostPauseAndBroadcast()
    {
        SetBattlePaused(true);
        NetworkManager.Instance?.Broadcast(new BattlePauseApplyMSG());
        Logger.Info("[BattlePause] Host paused battle.", LogTag.Battle);
    }

    void HostResumeAndBroadcast()
    {
        SetBattlePaused(false);
        NetworkManager.Instance?.Broadcast(new BattlePauseResumeMSG());
        Logger.Info("[BattlePause] Host resumed battle.", LogTag.Battle);
    }

    public void ClientApplyBattlePause()
    {
        SetBattlePaused(true);
    }

    public void ClientApplyBattleResume()
    {
        SetBattlePaused(false);
    }

    /// <summary>房主在联机手动暂停菜单选择返回房间：广播后本机与其它玩家一并回房。</summary>
    public void HostRequestReturnToRoomFromPause()
    {
        if (isSinglePlayerMode || !CanResumeBattle || PauseReason != E_BattlePauseReason.Manual)
            return;

        NetworkManager.Instance?.Broadcast(new BattlePauseReturnToRoomMSG());
        Logger.Info("[BattlePause] Host returning to room from pause.", LogTag.Battle);
        QuitBattleToRoomAsync().Forget();
    }

    public void ClientApplyBattleReturnToRoom()
    {
        if (isSinglePlayerMode)
            return;

        Logger.Info("[BattlePause] Following host return to room.", LogTag.Battle);
        QuitBattleToRoomAsync().Forget();
    }

    byte BuildActivePlayerMask()
    {
        byte mask = 0;
        for (int i = 0; i < _activePlayers.Length; i++)
        {
            if (_activePlayers[i])
                mask |= (byte)(1 << i);
        }
        return mask;
    }

    bool IsPlayerIndexActive(byte playerIndex)
    {
        return playerIndex < _activePlayers.Length && _activePlayers[playerIndex];
    }

    #endregion

    /// <summary>从暂停菜单退出战斗，回到标题场景主菜单。</summary>
    public async Task QuitBattleToMenuAsync()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ClosePanel<BattleUIPanel>();

        CurrentStatus = E_BattleStatus.Prepare;
        EndBattleSession();

        if (UIManager.Instance != null)
            UIManager.Instance.CloseAll();

        if (!await SceneLoader.LoadSceneAsync("TitleScene"))
        {
            Logger.Error("[Battle] Failed to load TitleScene after quit.", LogTag.Battle);
            return;
        }

        if (UIManager.Instance != null)
            await UIManager.Instance.ShowPanelAsync<MenuPanel>();
    }

    /// <summary>联机全员 Game Over 后返回房间界面（保持网络连接）。</summary>
    public async Task QuitBattleToRoomAsync()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.ClosePanel<BattleUIPanel>();

        CurrentStatus = E_BattleStatus.Prepare;
        EndBattleSession();

        if (UIManager.Instance != null)
            UIManager.Instance.CloseAll();

        if (!await SceneLoader.LoadSceneAsync("TitleScene"))
        {
            Logger.Error("[Battle] Failed to load TitleScene after quit to room.", LogTag.Battle);
            return;
        }

        if (UIManager.Instance != null)
            await UIManager.Instance.ShowPanelAsync<RoomPanel>();
    }

    public bool IsMultiplayerPrepare =>
        NetworkManager.Instance != null
        && NetworkManager.Instance.NetworkRole != NetworkRole.None;

    public bool IsPrepareCharacterAvailable(E_Character character, byte forPlayerIndex) =>
        !IsMultiplayerPrepare || _prepareCharacterRegistry.IsAvailable(character, forPlayerIndex);

    public bool IsPlayerPrepareCharacterLocked(byte playerIndex) =>
        _prepareCharacterRegistry.GetPick(playerIndex) != E_Character.None;

    public bool TryGetPrepareCharacterLocker(E_Character character, out byte playerIndex) =>
        _prepareCharacterRegistry.TryGetLocker(character, out playerIndex);

    /// <summary>确认准备时锁定角色；失败表示已被其他已准备玩家占用。</summary>
    public bool TryLockPrepareCharacter(byte playerIndex, E_Character character) =>
        character != E_Character.None && _prepareCharacterRegistry.TryClaim(playerIndex, character);

    /// <summary>房主本地确认准备：锁定、写入数据并广播。</summary>
    public bool HostSubmitPrepareReady(PlayerBattleData data)
    {
        if (!TryValidateAndLockPrepareReady(data))
            return false;

        SetOrUpdatePlayerData(data);
        NetworkManager.Instance.Broadcast(new BattleReadyMSG { playerBattleData = data });
        return true;
    }

    /// <summary>房主收到客户端的准备消息。</summary>
    public bool HostReceiveClientPrepareReady(PlayerBattleData data)
    {
        if (!TryValidateAndLockPrepareReady(data))
            return false;

        SetOrUpdatePlayerData(data);
        NetworkManager.Instance.Broadcast(new BattleReadyMSG { playerBattleData = data });
        Logger.Info($"[BattlePrepare] Player {data.playerIndex} ready: {data.characterId} / {data.weaponId}", LogTag.Battle);
        return true;
    }

    /// <summary>客户端收到房主广播的准备消息（含己方）。</summary>
    public void ClientApplyPrepareReadyBroadcast(PlayerBattleData data)
    {
        if (data.characterId != E_Character.None)
            _prepareCharacterRegistry.TryClaim(data.playerIndex, data.characterId);
        SetOrUpdatePlayerData(data);
    }

    public bool HostSubmitPrepareCancel(byte playerIndex)
    {
        RemovePreparePlayerData(playerIndex);
        NetworkManager.Instance.Broadcast(new BattlePrepareCancelMSG { playerIndex = playerIndex });
        return true;
    }

    public bool HostReceiveClientPrepareCancel(byte playerIndex)
    {
        RemovePreparePlayerData(playerIndex);
        NetworkManager.Instance.Broadcast(new BattlePrepareCancelMSG { playerIndex = playerIndex });
        return true;
    }

    public void ClientApplyPrepareCancelBroadcast(byte playerIndex)
    {
        RemovePreparePlayerData(playerIndex);
    }

    public void RemovePreparePlayerData(byte playerIndex)
    {
        _prepareCharacterRegistry.Release(playerIndex);
        for (int i = allPlayerDatas.Count - 1; i >= 0; i--)
        {
            if (allPlayerDatas[i].playerIndex == playerIndex)
                allPlayerDatas.RemoveAt(i);
        }
        OnPrepareCharacterReleased?.Invoke(playerIndex);
    }

    bool TryValidateAndLockPrepareReady(PlayerBattleData data)
    {
        if (data.characterId == E_Character.None || data.weaponId == E_Weapon.None)
        {
            Logger.Warn($"[BattlePrepare] Invalid ready data from player {data.playerIndex}.", LogTag.Battle);
            return false;
        }

        if (!IsMultiplayerPrepare)
            return true;

        if (TryLockPrepareCharacter(data.playerIndex, data.characterId))
            return true;

        Logger.Warn(
            $"[BattlePrepare] Character {data.characterId} already locked by another ready player.",
            LogTag.Battle);
        return false;
    }

    static bool ValidateUniqueCharacterPicks(IReadOnlyList<PlayerBattleData> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            E_Character a = players[i].characterId;
            if (a == E_Character.None)
                return false;
            for (int j = i + 1; j < players.Count; j++)
            {
                if (players[j].characterId == a)
                    return false;
            }
        }
        return true;
    }

    void EnsureGlobalBattleDataReady()
    {
        if (GlobalBattleData.IsInitialized || !GameResDB.IsInitialized)
            return;

        var manifest = ResManager.Instance?.Manifest;
        if (manifest == null || string.IsNullOrEmpty(manifest.battleAreaConfigId))
            return;

        var battleArea = GameResDB.Instance.GetConfig<BattleAreaConfig>(manifest.battleAreaConfigId);
        if (battleArea != null)
            GlobalBattleData.Initialize(battleArea);
    }

    #endregion

    #region 开战（ECS Bootstrap）

    public void StartSinglePlayerBattle()
    {
        BeginBattleSession(singlePlayer: true, logicStartFrame: 0, remotePlayerDatas: null);
    }

    public void StartMutiPlayerBattleForHost()
    {
        var playerDatas = allPlayerDatas.ToArray();
        uint startFrame = 0;
        uint randomSeed = 0;

        NetworkManager.Instance.Broadcast(new BattleStartMSG
        {
            startFrame = startFrame,
            randomSeed = randomSeed,
            playerDatas = playerDatas
        });

        BeginBattleSession(singlePlayer: false, startFrame, playerDatas);
    }

    public void StartMutiPlayerBattleForClient(uint startFrame, uint randomSeed, PlayerBattleData[] playerDatas)
    {
        BeginBattleSession(singlePlayer: false, startFrame, playerDatas);
    }

    /// <summary>单机 / 联机开战的唯一入口（在 Bootstrap 之前整理玩家与输入状态）。</summary>
    void BeginBattleSession(bool singlePlayer, uint logicStartFrame, PlayerBattleData[] remotePlayerDatas)
    {
        isSinglePlayerMode = singlePlayer;
        PresentationRuntime.SetSmoothingEnabled(!singlePlayer);

        if (remotePlayerDatas != null)
        {
            allPlayerDatas.Clear();
            for (int i = 0; i < remotePlayerDatas.Length; i++)
                AddPlayerData(remotePlayerDatas[i]);
        }

        if (allPlayerDatas.Count == 0)
        {
            Logger.Error("[Battle] No player data; cannot start session.", LogTag.Battle);
            return;
        }

        if (!singlePlayer && !ValidateUniqueCharacterPicks(allPlayerDatas))
        {
            Logger.Error("[Battle] Duplicate character picks among players; cannot start session.", LogTag.Battle);
            return;
        }

        EnsureGlobalBattleDataReady();
        if (!GlobalBattleData.IsInitialized)
        {
            Logger.Critical("[Battle] GlobalBattleData not initialized; aborting session start.", LogTag.Battle);
            return;
        }

        RebuildActivePlayerMask();
        if (InputManager.Instance != null)
            InputManager.Instance.ClearAllInputs();
        BootstrapBattleSession(logicStartFrame);
        InputManager.Instance?.PrepareLockstepInputBuffer(logicStartFrame, singlePlayer, _activePlayers);
    }

    void RebuildActivePlayerMask()
    {
        Array.Clear(_activePlayers, 0, _activePlayers.Length);
        for (int i = 0; i < allPlayerDatas.Count; i++)
            _activePlayers[allPlayerDatas[i].playerIndex] = true;
    }

    /// <summary>
    /// ECS 世界创建顺序：释放旧世界 → 战斗区+池 → 新世界 → 逻辑帧 → 时间轴 → 玩家 → InBattle → HUD。
    /// </summary>
    void BootstrapBattleSession(uint logicStartFrame)
    {
        CurrentStatus = E_BattleStatus.Prepare;
        GlobalBattleData.ResetBattleSessionStats();
        DisposeBattleWorld();
        PrepareBattleInfrastructure();
        CreateBattleWorld();
        _battleWorld.LogicFrameTimer.ResetToFrame(logicStartFrame);
        TryBeginStageTimeline();
        GeneratePlayer();
        IsBattlePaused = false;
        _pauseReason = E_BattlePauseReason.None;
        _eliminatedPlayerMask = 0;
        _stageClearHandled = false;
        CurrentStatus = E_BattleStatus.InBattle;
        BattleRuntimeMetrics.Reset();
        ShowBattleUIPanelFireAndForget();
    }

    #endregion

    public void SetActiveStageTimelineConfigId(string configId)
    {
        if (string.IsNullOrEmpty(configId)) return;
        _activeStageTimelineConfigId = configId.ToLowerInvariantTrimmed();
    }

    StageTimelineConfig ResolveStageTimelineForBattle()
    {
        if (!GameResDB.IsInitialized)
            return null;
        return GameResDB.Instance.GetConfig<StageTimelineConfig>(_activeStageTimelineConfigId);
    }

    public bool TryGetStageState(out E_StageState state)
    {
        state = E_StageState.None;
        if (_battleWorld == null) return false;
        var timeline = _battleWorld.GetSystem<StageTimelineSystem>();
        return timeline != null && timeline.TryGetStageState(out state);
    }

    /// <summary>关底 Boss 登场（<see cref="E_StageState.BossIntro"/>）或中场 Boss 在场时返回 true。</summary>
    public bool TryGetBossHudSnapshot(out BossHudSnapshot snap)
    {
        snap = default;
        if (_battleWorld == null || CurrentStatus != E_BattleStatus.InBattle)
            return false;

        var timeline = _battleWorld.GetSystem<StageTimelineSystem>();
        return timeline != null && timeline.TryGetBossHudSnapshot(out snap);
    }

    void CreateBattleWorld()
    {
        _battleWorld = new World();
        _battleWorld.AddSystem<StageTimelineSystem>();
        _battleWorld.AddSystem<MidBossEncounterSystem>();
        _battleWorld.AddSystem<MainBossEncounterSystem>();
        _battleWorld.AddSystem<EnemyMovementSystem>();
        _battleWorld.AddSystem<DropItemSystem>();
        _battleWorld.AddSystem<CollisionSystem>();
        _battleWorld.AddSystem<CollisionLogicSystem>();
        _battleWorld.AddSystem<PlayerRespawnSystem>();
        _battleWorld.AddSystem<PlayerControlSystem>();
        _battleWorld.AddSystem<DropItemCollectSystem>();
        _battleWorld.AddSystem<DropItemMagnetSystem>();
        _battleWorld.AddSystem<DanmakuSystem>();
        _battleWorld.AddSystem<DanmakuEmitSystem>();
        if (!isSinglePlayerMode)
            _battleWorld.AddSystem<PresentationPoseSystem>();
        _battleWorld.AddSystem<PresentationSystem>();
        Logger.Info("Battle ECS World initialized.");
    }

    void DisposeBattleWorld()
    {
        EndBattleSession();
    }

    const string PrefabIdBattlePanel = "battlepanel";

    void ShowBattleUIPanelFireAndForget()
    {
        _ = ShowBattleUIPanelAsync();
    }

    async Task ShowBattleUIPanelAsync()
    {
        try
        {
            if (UIManager.Instance == null)
            {
                Logger.Error("[Battle] UIManager.Instance 为空，无法打开 BattleUIPanel。", LogTag.UI);
                return;
            }

            var panel = await UIManager.Instance.ShowPanelAsync<BattleUIPanel>();
            if (panel == null)
                Logger.Error($"[Battle] BattleUIPanel 未创建成功，请检查 prefab_{PrefabIdBattlePanel} 与 UIManager 日志。", LogTag.UI);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Battle] Failed to open BattleUIPanel (prefab_{PrefabIdBattlePanel}): {ex.Message}", LogTag.UI);
        }
    }

    static int ResolveCharacterMaxHealth(byte playerIndex)
    {
        if (BattleManager.Instance == null
            || !BattleManager.Instance.TryGetPlayerBattleData(playerIndex, out var data))
        {
            return 0;
        }

        string characterId = StringHelper.NormalizeResourceId(data.characterId.ToString());
        var cfg = GameResDB.Instance?.GetConfig<CharacterConfig>(characterId);
        return cfg != null ? cfg.maxHealth : 0;
    }

    public bool TryGetBattleHudSnapshot(out BattleHudSnapshot snap)
    {
        snap = default;
        if (_battleWorld == null || CurrentStatus != E_BattleStatus.InBattle)
            return false;

        int score = GlobalBattleData.SessionScore;

        byte localIdx = RoomManager.LocalPlayerIndex;
        if (IsPlayerEliminated(localIdx))
        {
            int maxHp = ResolveCharacterMaxHealth(localIdx);
            snap = new BattleHudSnapshot(score, 0, maxHp, 0);
            return true;
        }

        var em = _battleWorld.EntityManager;
        Span<int> playerIndices = em.GetActiveIndices<CPlayer>();
        if (playerIndices.Length == 0)
        {
            int maxHp = ResolveCharacterMaxHealth(localIdx);
            snap = new BattleHudSnapshot(score, 0, maxHp, 0);
            return true;
        }

        int chosen = playerIndices[0];
        for (int i = 0; i < playerIndices.Length; i++)
        {
            int idx = playerIndices[i];
            ref readonly var p = ref em.GetComponentSpan<CPlayer>()[idx];
            if (p.playerIndex == localIdx)
            {
                chosen = idx;
                break;
            }
        }

        ref readonly var pl = ref em.GetComponentSpan<CPlayer>()[chosen];
        ref readonly var hp = ref em.GetComponentSpan<CHealth>()[chosen];
        snap = new BattleHudSnapshot(score, hp.currentHealth, hp.maxHealth, pl.powerOrbs);
        return true;
    }

    public bool TryGetBattleRuntimeSnapshot(out BattleRuntimeSnapshot snap)
    {
        snap = default;
        if (_battleWorld == null || CurrentStatus != E_BattleStatus.InBattle)
            return false;

        snap = new BattleRuntimeSnapshot(
            BattleRuntimeMetrics.RenderFps,
            BattleRuntimeMetrics.LogicFps,
            _battleWorld.EntityManager.ActiveEntityCount,
            _battleWorld.GameObjectBridge.LinkedGameObjectCount);
        return true;
    }

    void TryBeginStageTimeline()
    {
        if (_battleWorld == null) return;
        var cfg = ResolveStageTimelineForBattle();
        if (cfg == null)
        {
            Logger.Warn($"[Battle] Stage timeline not resolved (id='{_activeStageTimelineConfigId}'). Register in GameResourceManifest.stageTimelineConfigIds.", LogTag.Battle);
            return;
        }
        _battleWorld.GetSystem<StageTimelineSystem>()?.Begin(cfg);
        GlobalBattleData.ApplyStageTimeline(cfg);
        BattleStageBackgroundPresenter.EnsureFromGlobalBattleData();
    }

    void PrepareBattleInfrastructure()
    {
        EnsureGlobalBattleDataReady();
        var globalPoolConfig = GameResDB.Instance.GetConfig<GlobalPoolConfig>("defaultglobalpool");
        WarmupGlobalPools(globalPoolConfig);
    }

    void WarmupGlobalPools(GlobalPoolConfig globalPoolConfig)
    {
        if (globalPoolConfig == null)
        {
            Logger.Warn("[Battle] GlobalPoolConfig 'defaultglobalpool' not found.", LogTag.Pool);
            return;
        }

        int maxPrefabIndex = GameResDB.Instance.GetMaxPrefabIndex();
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

    #region 玩家与测试

    public void AddPlayerData(PlayerBattleData playerData)
    {
        allPlayerDatas.Add(playerData);
        _activePlayers[playerData.playerIndex] = true;
    }

    /// <summary>准备阶段重复确认时按 playerIndex 覆盖，避免重复条目。</summary>
    public void SetOrUpdatePlayerData(PlayerBattleData playerData)
    {
        if (playerData.characterId != E_Character.None)
            _prepareCharacterRegistry.TryClaim(playerData.playerIndex, playerData.characterId);

        for (int i = 0; i < allPlayerDatas.Count; i++)
        {
            if (allPlayerDatas[i].playerIndex != playerData.playerIndex)
                continue;
            allPlayerDatas[i] = playerData;
            _activePlayers[playerData.playerIndex] = true;
            NotifyPrepareCharacterLocked(playerData.playerIndex, playerData.characterId);
            return;
        }
        AddPlayerData(playerData);
        NotifyPrepareCharacterLocked(playerData.playerIndex, playerData.characterId);
    }

    void NotifyPrepareCharacterLocked(byte playerIndex, E_Character characterId)
    {
        if (characterId == E_Character.None)
            return;
        OnPrepareCharacterLocked?.Invoke(playerIndex, characterId);
    }

    public void GeneratePlayer()
    {
        if (allPlayerDatas == null || allPlayerDatas.Count == 0)
        {
            Logger.Error("No player data available to create players.");
            return;
        }

        for (int i = 0; i < allPlayerDatas.Count; i++)
        {
            var playerData = allPlayerDatas[i];

            if (playerData.characterId == E_Character.None || playerData.weaponId == E_Weapon.None)
            {
                Logger.Error($"Invalid player data for player index {playerData.playerIndex}: characterId={playerData.characterId}, weaponId={playerData.weaponId}");
                continue;
            }
            InitializePlayerEntity(playerData);
        }
    }

    void InitializePlayerEntity(PlayerBattleData playerData)
    {
        var bornPos = GlobalBattleData.SpawnData.GetPlayerSpawnPos(playerData.playerIndex, TotalPlayers);
        Logger.Debug($"Spawning player {playerData.playerIndex} at position ({bornPos.x}, {bornPos.y})");
        var e_Player = _battleWorld.EntityFactory.CreatePlayer(playerData, bornPos.x, bornPos.y);
        _battleWorld.EntityManager.AddComponent(e_Player, new CPoolGetTag());
    }

    public void AddEnemyTest(EnemyConfig enemyConfig, float posX, float posY)
    {
        var e_enemy = _battleWorld.EntityFactory.CreateEnemy(enemyConfig, posX, posY);
        uint f = _battleWorld.LogicFrameTimer.CurrentFrame;
        _battleWorld.EntityManager.AddComponent(e_enemy, EnemyMovementBaking.CreateSimpleDescent(f, posX, posY));
        _battleWorld.EntityManager.AddComponent(e_enemy, new CPoolGetTag());
        Logger.Info($"Test enemy added at ({posX}, {posY}) with config index {enemyConfig.emitterConfigIndex}.");
    }

    /// <summary>单元测试：设置指定玩家的火力（powerOrbs），并同步副炮布局。</summary>
    public bool TrySetPlayerPowerOrbs(byte playerIndex, int powerOrbs)
    {
        if (_battleWorld == null || CurrentStatus != E_BattleStatus.InBattle)
            return false;

        powerOrbs = Math.Max(0, powerOrbs);

        var em = _battleWorld.EntityManager;
        Span<int> playerIndices = em.GetActiveIndices<CPlayer>();
        if (playerIndices.Length == 0)
            return false;

        var players = em.GetComponentSpan<CPlayer>();
        Entity targetEntity = Entity.Null;

        for (int i = 0; i < playerIndices.Length; i++)
        {
            int entityIdx = playerIndices[i];
            if (players[entityIdx].playerIndex != playerIndex)
                continue;

            targetEntity = em.GetEntity(entityIdx);
            break;
        }

        if (!em.IsValid(targetEntity))
            return false;

        ref var player = ref em.GetComponent<CPlayer>(targetEntity);
        player.powerOrbs = powerOrbs;

        var weaponConfig = GameResDB.Instance.GetConfig<WeaponConfig>(player.weaponCfgIndex);
        if (weaponConfig != null)
            _battleWorld.EntityFactory.SyncPlayerSecondaryEmitters(targetEntity, weaponConfig, powerOrbs);

        Logger.Info($"[UnitTest] Player {playerIndex} powerOrbs = {powerOrbs}.", LogTag.UnitTest);
        return true;
    }

    #endregion

    void Update()
    {
        if (_battleWorld == null) return;
        if (CurrentStatus != E_BattleStatus.InBattle) return;

        if (IsBattlePaused) return;

        _battleWorld.LogicFrameTimer.AccumulateDeltaTime(Time.unscaledDeltaTime);

        int maxCatchUpSteps = isSinglePlayerMode ? 8 : 8;
        int steps = 0;
        bool logicStalledThisRenderFrame = false;
        while (steps < maxCatchUpSteps && _battleWorld.LogicFrameTimer.CanAdvance())
        {
            uint frameToProcess = _battleWorld.LogicFrameTimer.CurrentFrame;
            uint captureFrame = InputManager.Instance.ResolveCaptureFrame(frameToProcess, isSinglePlayerMode);

            byte localIndex = RoomManager.LocalPlayerIndex;
            byte eliminatedMask = _eliminatedPlayerMask;

            if (!isSinglePlayerMode)
                InputManager.Instance.FillNeutralInputsForEliminated(frameToProcess, _activePlayers, eliminatedMask);

            if (IsLocalSpectating)
            {
                InputManager.Instance.WriteNeutralInputForPlayer(localIndex, captureFrame);
            }
            else
            {
                InputManager.Instance.RecordLocalInput(localIndex, captureFrame);
            }

            if (!isSinglePlayerMode)
                InputManager.Instance.BroadcastInputWindow(localIndex, frameToProcess, captureFrame);

            if (isSinglePlayerMode
                || InputManager.Instance.AreAllInputsReady(frameToProcess, _activePlayers, eliminatedMask))
            {
                _battleWorld.LogicTick(frameToProcess);
                _battleWorld.LogicFrameTimer.AdvanceFrame();
                _battleWorld.LogicFrameTimer.ConsumeFrameTime();
                InputManager.Instance.NotifyLogicTickSucceeded();
                steps++;
            }
            else
            {
                logicStalledThisRenderFrame = true;
                InputManager.Instance.NotifyLogicTickStalled(frameToProcess, _activePlayers, eliminatedMask);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Logger.Debug(
                    $"[Frame {frameToProcess}] Time ready but inputs not ready (missing P{InputManager.Instance.LastStalledPlayerIndex}).",
                    LogTag.Battle);
#endif
                break;
            }
        }

        if (!isSinglePlayerMode)
            PresentationRuntime.Sync(_battleWorld.LogicFrameTimer, logicStalledThisRenderFrame);
        BattleRuntimeMetrics.RecordLogicTicks(steps);
        _battleWorld.Update(Time.deltaTime);
    }

    void LateUpdate()
    {
        if (_battleWorld == null) return;
        if (CurrentStatus != E_BattleStatus.InBattle) return;
        if (IsBattlePaused) return;

        _battleWorld.LateUpdate(Time.deltaTime);
    }
}
