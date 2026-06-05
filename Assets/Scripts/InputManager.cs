using System;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct FrameInput
{
    public uint frame;
    public byte playerIndex;
    public byte directionPacked; // [H:2bits][V:2bits][--:4bits]
    public byte buttons;         // [shoot][bomb][slow][any][----]

    // --- 解包属性 ---
    public readonly sbyte MoveHorizontal => (sbyte)((directionPacked & 0x3) - 1);
    public readonly sbyte MoveVertical => (sbyte)(((directionPacked >> 2) & 0x3) - 1);
    public readonly bool Shoot => (buttons & 0x01) != 0;
    public readonly bool Bomb => (buttons & 0x02) != 0;
    public readonly bool SlowMode => (buttons & 0x04) != 0;
    public readonly bool AnyKey => (buttons & 0x08) != 0;

    // --- 构造 ---
    public static FrameInput Create(
        uint frame, byte playerIndex,
        sbyte h, sbyte v,
        bool shoot, bool bomb, bool slow, bool anyKey)
    {
        // Clamp to [-1, 1] just in case
        h = (sbyte)Mathf.Clamp(h, -1, 1);
        v = (sbyte)Mathf.Clamp(v, -1, 1);

        byte dir = (byte)((((v + 1) << 2) | (h + 1)) & 0xF);
        byte btn = (byte)(
            (shoot ? 1 : 0) |
            (bomb ? 2 : 0) |
            (slow ? 4 : 0) |
            (anyKey ? 8 : 0)
        );

        return new FrameInput
        {
            frame = frame,
            playerIndex = playerIndex,
            directionPacked = dir,
            buttons = btn
        };
    }

    public static FrameInput None => Create(0, 0, 0, 0, false, false, false, false);
}

#region 键位配置

[Serializable]
public class InputKeyCodeConfig
{
    public KeyCode moveLeft = KeyCode.LeftArrow;
    public KeyCode moveRight = KeyCode.RightArrow;
    public KeyCode moveUp = KeyCode.UpArrow;
    public KeyCode moveDown = KeyCode.DownArrow;
    public KeyCode shoot = KeyCode.Z;
    public KeyCode bomb = KeyCode.X;
    public KeyCode slow = KeyCode.LeftShift;
    public KeyCode pause = KeyCode.Escape;
}

#endregion

public class InputManager : SingletonMono<InputManager>
{
    const int MAX_PLAYERS = 4;
    // 【关键优化 1】环形缓冲区大小
    // 假设最大网络延迟 + 重传窗口约 2 秒（120 帧 @ 60fps）。
    // 即使延迟达到 1 秒，只要 buffer 足够大，旧数据被覆盖前肯定已经被消费了。
    // 设为 256 (2 的幂) 可以让编译器优化 % 运算为位运算 (& 255)，性能极致。
    const int BUFFER_SIZE = 256;
    const int BUFFER_MASK = BUFFER_SIZE - 1; // 用于快速取模

    InputKeyCodeConfig _inputKeyCodeCfg = new();

    // 【关键优化 2】改用二维数组代替 Dictionary 数组
    // _inputFrames[playerIndex][frame % BUFFER_SIZE]
    private FrameInput[][] _inputFrames;

    // 记录每个玩家当前已收到的最大帧号，用于快速判断是否就绪
    private uint[] _latestReceivedFrame;

    FrameInput[] _currentConsumedInputs;
    bool _isInitialized = false;

    const uint InvalidFrameSlot = uint.MaxValue;
    const int SyncStatsWindowSize = 300;
    const int MaxReasonableInputDelayFrames = 8;

    [Header("联机锁步")]
    [SerializeField]
    [Tooltip("联机锁步输入前瞻帧数。值越高越抗网络抖动，但操作延迟会增加。默认 1 帧。")]
    [Range(0, MaxReasonableInputDelayFrames)]
    int _multiplayerInputDelayFrames = 2;

    int _logicTickSuccessCount;
    int _logicTickStallCount;
    uint _lastStalledLogicFrame;
    int _lastStalledPlayerIndex = -1;
    readonly bool[] _syncStallWindow = new bool[SyncStatsWindowSize];
    int _syncWindowWriteIndex;
    int _syncWindowCount;
    int _syncWindowStallCount;

    protected override void OnSingletonInit()
    {
        base.OnSingletonInit();
        InitializeForGame();
    }

    public InputKeyCodeConfig KeyConfig => _inputKeyCodeCfg;

    public void ApplyKeyConfig(InputKeyCodeConfig config)
    {
        if (config == null) return;
        _inputKeyCodeCfg.moveLeft = config.moveLeft;
        _inputKeyCodeCfg.moveRight = config.moveRight;
        _inputKeyCodeCfg.moveUp = config.moveUp;
        _inputKeyCodeCfg.moveDown = config.moveDown;
        _inputKeyCodeCfg.shoot = config.shoot;
        _inputKeyCodeCfg.bomb = config.bomb;
        _inputKeyCodeCfg.slow = config.slow;
        _inputKeyCodeCfg.pause = config.pause;
    }

    public void InitializeForGame()
    {
        if (GameSettingsService.Instance != null)
            GameSettingsService.Instance.EnsureLoaded();
        if (GameSettingsService.Instance != null)
            ApplyKeyConfig(GameSettingsService.Instance.Data.keyBindings);
        else
            _inputKeyCodeCfg = new InputKeyCodeConfig();

        // 初始化环形缓冲
        _inputFrames = new FrameInput[MAX_PLAYERS][];
        _latestReceivedFrame = new uint[MAX_PLAYERS];
        _currentConsumedInputs = new FrameInput[MAX_PLAYERS];

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            _inputFrames[i] = new FrameInput[BUFFER_SIZE];
            // 初始化为 None (默认 struct 值通常是 0，相当于 None，但为了保险可以显式填充)
            // Array.Fill(_inputFrames[i], FrameInput.None); // Unity 2020+ 支持，或者用循环
            for (int j = 0; j < BUFFER_SIZE; j++)
                _inputFrames[i][j] = CreateInvalidSlot();

            _latestReceivedFrame[i] = 0;
            _currentConsumedInputs[i] = FrameInput.None;
        }

        _isInitialized = true;
    }

    public void ClearAllInputs()
    {
        if (!_isInitialized) return;
        ResetSyncDiagnostics();
        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            _latestReceivedFrame[i] = 0;
            for (int j = 0; j < BUFFER_SIZE; j++)
                _inputFrames[i][j] = CreateInvalidSlot();
        }
    }

    static FrameInput CreateInvalidSlot()
    {
        return new FrameInput
        {
            frame = InvalidFrameSlot,
            playerIndex = 0,
            directionPacked = 0,
            buttons = 0
        };
    }

    public void ResetSyncDiagnostics()
    {
        _logicTickSuccessCount = 0;
        _logicTickStallCount = 0;
        _lastStalledLogicFrame = 0;
        _lastStalledPlayerIndex = -1;
        _syncWindowWriteIndex = 0;
        _syncWindowCount = 0;
        _syncWindowStallCount = 0;
        Array.Clear(_syncStallWindow, 0, _syncStallWindow.Length);
    }

    public void NotifyLogicTickStalled(uint logicFrame, bool[] activePlayers, byte eliminatedMask = 0)
    {
        _logicTickStallCount++;
        RecordSyncSample(stalled: true);
        _lastStalledLogicFrame = logicFrame;
        _lastStalledPlayerIndex = TryFindFirstMissingPlayer(logicFrame, activePlayers, eliminatedMask);
    }

    public void NotifyLogicTickSucceeded()
    {
        _logicTickSuccessCount++;
        RecordSyncSample(stalled: false);
    }

    void RecordSyncSample(bool stalled)
    {
        if (_syncWindowCount == SyncStatsWindowSize)
        {
            if (_syncStallWindow[_syncWindowWriteIndex])
                _syncWindowStallCount--;
        }
        else
        {
            _syncWindowCount++;
        }

        _syncStallWindow[_syncWindowWriteIndex] = stalled;
        if (stalled)
            _syncWindowStallCount++;

        _syncWindowWriteIndex++;
        if (_syncWindowWriteIndex >= SyncStatsWindowSize)
            _syncWindowWriteIndex = 0;
    }

    public float RecentStallRatio
    {
        get
        {
            int total = _syncWindowCount;
            return total > 0 ? (float)_syncWindowStallCount / total : 0f;
        }
    }

    public int RecentStallCount => _syncWindowStallCount;
    public int RecentSuccessCount => _syncWindowCount - _syncWindowStallCount;
    public int RecentSampleCount => _syncWindowCount;
    public int MultiplayerInputDelayFrames => Mathf.Clamp(_multiplayerInputDelayFrames, 0, MaxReasonableInputDelayFrames);

    public uint LastStalledLogicFrame => _lastStalledLogicFrame;
    public int LastStalledPlayerIndex => _lastStalledPlayerIndex;

    public uint ResolveCaptureFrame(uint logicFrameToProcess, bool singlePlayerMode)
    {
        if (singlePlayerMode)
            return logicFrameToProcess;

        return logicFrameToProcess + (uint)MultiplayerInputDelayFrames;
    }

    /// <summary>
    /// 开局预热前瞻输入窗口，保证锁步起始若干帧不因“未来输入”缺失而卡住。
    /// 预热帧会写入中性输入；真实输入从 <see cref="ResolveCaptureFrame"/> 产出的未来帧开始生效。
    /// </summary>
    public void PrepareLockstepInputBuffer(uint startLogicFrame, bool singlePlayerMode, bool[] activePlayers)
    {
        if (!_isInitialized || singlePlayerMode || activePlayers == null)
            return;

        int delay = MultiplayerInputDelayFrames;
        if (delay <= 0)
            return;

        for (int p = 0; p < MAX_PLAYERS; p++)
        {
            if (!activePlayers[p])
                continue;

            for (int i = 0; i < delay; i++)
            {
                uint warmupFrame = startLogicFrame + (uint)i;
                WriteNeutralInputForPlayer((byte)p, warmupFrame);
            }
        }
    }

    int TryFindFirstMissingPlayer(uint logicFrame, bool[] activePlayers, byte eliminatedMask = 0)
    {
        if (!_isInitialized || activePlayers == null)
            return -1;

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (!activePlayers[i])
                continue;

            if ((eliminatedMask & (1 << i)) != 0)
                continue;

            if (_latestReceivedFrame[i] < logicFrame)
                return i;

            int index = (int)(logicFrame & BUFFER_MASK);
            if (_inputFrames[i][index].frame != logicFrame)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// 已淘汰玩家不再发网包时，各端本地注入中性输入，避免锁步永久等待。
    /// </summary>
    public void FillNeutralInputsForEliminated(uint logicFrame, bool[] activePlayers, byte eliminatedMask)
    {
        if (!_isInitialized || activePlayers == null || eliminatedMask == 0)
            return;

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (!activePlayers[i] || (eliminatedMask & (1 << i)) == 0)
                continue;

            WriteNeutralInputForPlayer((byte)i, logicFrame);
        }
    }

    public void WriteNeutralInputForPlayer(byte playerIndex, uint logicFrame)
    {
        if (!_isInitialized || playerIndex >= MAX_PLAYERS)
            return;

        int index = (int)(logicFrame & BUFFER_MASK);
        _inputFrames[playerIndex][index] = FrameInput.Create(
            logicFrame, playerIndex, 0, 0, false, false, false, false);

        if (logicFrame > _latestReceivedFrame[playerIndex])
            _latestReceivedFrame[playerIndex] = logicFrame;
    }

    // --- 核心逻辑修改 ---
    /// <summary>采样当前键位（不写环形缓冲），供表现层本地预测使用。</summary>
    public FrameInput SampleLocalInput(byte playerIndex, uint logicFrame)
    {
        if (!_isInitialized || playerIndex >= MAX_PLAYERS)
            return FrameInput.None;

        return FrameInput.Create(
            logicFrame,
            playerIndex,
            (sbyte)(Input.GetKey(_inputKeyCodeCfg.moveRight) ? 1 : Input.GetKey(_inputKeyCodeCfg.moveLeft) ? -1 : 0),
            (sbyte)(Input.GetKey(_inputKeyCodeCfg.moveUp) ? 1 : Input.GetKey(_inputKeyCodeCfg.moveDown) ? -1 : 0),
            Input.GetKey(_inputKeyCodeCfg.shoot),
            Input.GetKey(_inputKeyCodeCfg.bomb),
            Input.GetKey(_inputKeyCodeCfg.slow),
            Input.anyKey);
    }

    public FrameInput RecordLocalInput(byte playerIndex, uint logicFrame)
    {
        if (!_isInitialized || playerIndex >= MAX_PLAYERS) return FrameInput.None;

        // 【优化】检查是否已存在：通过比较帧号
        // 在环形缓冲中，如果当前帧 <= 最新帧，且差值在缓冲区内，说明已存在
        // 但为了简单和安全，我们可以直接写入，或者检查该位置是否已经是这一帧
        int index = (int)(logicFrame & BUFFER_MASK);
        var existing = _inputFrames[playerIndex][index];

        if (existing.frame == logicFrame)
        {
            // 已经记录过这一帧了
            return existing;
        }

        var input = SampleLocalInput(playerIndex, logicFrame);

        // 直接写入，自动覆盖 BUFFER_SIZE 之前的旧数据 (零 GC!)
        _inputFrames[playerIndex][index] = input;

        // 更新最大帧号
        if (logicFrame > _latestReceivedFrame[playerIndex])
        {
            _latestReceivedFrame[playerIndex] = logicFrame;
        }

        return input;
    }

    public void BroadcastLocalInput(FrameInput input)
    {
        // ... (保持不变) ...
        if (NetworkManager.Instance.NetworkRole == NetworkRole.Client)
        {
            NetworkManager.Instance.SendToHost(new InputMSG { frameInput = input });
        }
        else if (NetworkManager.Instance.NetworkRole == NetworkRole.Host)
        {
            NetworkManager.Instance.Broadcast(new InputMSG { frameInput = input });
        }
    }

    /// <summary>
    /// 重发本地输入窗口，提升 UDP 丢包下的锁步恢复能力。
    /// 通常在 BattleManager 每次尝试推进时调用，范围为 [等待帧, 前瞻采集帧]。
    /// </summary>
    public void BroadcastInputWindow(byte playerIndex, uint minFrameInclusive, uint maxFrameInclusive)
    {
        if (!_isInitialized || playerIndex >= MAX_PLAYERS)
            return;

        var net = NetworkManager.Instance;
        if (net == null || net.NetworkRole == NetworkRole.None)
            return;

        if (maxFrameInclusive < minFrameInclusive)
            return;

        uint frame = minFrameInclusive;
        while (true)
        {
            int index = (int)(frame & BUFFER_MASK);
            var input = _inputFrames[playerIndex][index];
            if (input.frame == frame)
                BroadcastLocalInput(input);

            if (frame == maxFrameInclusive || frame == uint.MaxValue)
                break;
            frame++;
        }
    }

    public void AddRemoteInput(FrameInput input)
    {
        if (!_isInitialized || input.playerIndex >= MAX_PLAYERS) return;

        int index = (int)(input.frame & BUFFER_MASK);
        var existing = _inputFrames[input.playerIndex][index];

        // 冲突检测 (仅在开发版)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (existing.frame == input.frame && !StructEquals(existing, input))
        {
            Debug.LogWarning($"[Input] P{input.playerIndex} F{input.frame} conflict! Desync detected.");
        }
#endif
        // 写入 (允许覆盖，用于处理乱序到达或重传)
        _inputFrames[input.playerIndex][index] = input;

        if (input.frame > _latestReceivedFrame[input.playerIndex])
        {
            _latestReceivedFrame[input.playerIndex] = input.frame;
        }
    }

    static bool StructEquals(in FrameInput a, in FrameInput b)
    {
        return a.frame == b.frame &&
               a.playerIndex == b.playerIndex &&
               a.directionPacked == b.directionPacked &&
               a.buttons == b.buttons;
    }

    // --- 就绪检查优化 ---
    /// <param name="eliminatedMask">已生命归零、不再参与锁步的玩家位掩码。</param>
    public bool AreAllInputsReady(uint logicFrame, bool[] activePlayers, byte eliminatedMask = 0)
    {
        if (!_isInitialized || activePlayers == null) return false;

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (!activePlayers[i])
                continue;

            if ((eliminatedMask & (1 << i)) != 0)
                continue;

            if (_latestReceivedFrame[i] < logicFrame)
                return false;

            int index = (int)(logicFrame & BUFFER_MASK);
            if (_inputFrames[i][index].frame != logicFrame)
                return false;
        }

        return true;
    }
    public FrameInput GetInputForFrame(byte playerIndex, uint logicFrame)
    {
        if (!_isInitialized || playerIndex >= MAX_PLAYERS) return FrameInput.None;

        int index = (int)(logicFrame & BUFFER_MASK);
        var input = _inputFrames[playerIndex][index];

        // 验证帧号是否匹配 (防止读取到上一轮回绕的旧数据)
        if (input.frame == logicFrame)
        {
            _currentConsumedInputs[playerIndex] = input;
            return input;
        }

        // 没找到或数据过期
        _currentConsumedInputs[playerIndex] = FrameInput.None;
        return FrameInput.None;
    }



    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            UIManager.Instance.ToggleDebugPanelAsync().Forget();
        }
    }

    #region 调试显示（Canvas / TMP，避免 OnGUI 重绘）

    public FrameInput GetDebugInput(byte playerIndex)
    {
        if (!_isInitialized || playerIndex >= MAX_PLAYERS) return FrameInput.None;
        return _currentConsumedInputs[playerIndex];
    }

    [SerializeField]
    [Tooltip("在 UIManager 的 UICanvas 上用 TextMeshPro 显示各玩家最近消费输入；关闭时不创建/隐藏 HUD。")]
    bool _showDebugInput = true;

    InputDebugHud _inputDebugHud;
    readonly System.Text.StringBuilder _debugSb = new(256);

    void LateUpdate()
    {
        RefreshInputDebugHud();
    }

    void RefreshInputDebugHud()
    {
        if (!_isInitialized || !_showDebugInput)
        {
            if (_inputDebugHud != null)
                _inputDebugHud.SetVisible(false);
            return;
        }

        if (UIManager.Instance == null || UIManager.Instance.Canvas == null)
        {
            if (_inputDebugHud != null)
                _inputDebugHud.SetVisible(false);
            return;
        }

        _inputDebugHud ??= InputDebugHud.GetOrCreate(UIManager.Instance.Canvas.transform);
        if (_inputDebugHud == null)
            return;

        _inputDebugHud.SetVisible(true);

        _debugSb.Clear();

        var battle = BattleManager.Instance;
        if (battle != null
            && battle.CurrentStatus == E_BattleStatus.InBattle
            && !battle.isSinglePlayerMode)
        {
            float stallPct = RecentStallRatio * 100f;
            _debugSb.Append("<color=#ffaa66>锁步(最近")
                .Append(RecentSampleCount)
                .Append("样本): 等待 ")
                .Append(RecentStallCount)
                .Append(" / 推进 ")
                .Append(RecentSuccessCount)
                .Append(" (")
                .Append(stallPct.ToString("F0"))
                .Append("%)")
                .Append("  延迟缓冲:")
                .Append(MultiplayerInputDelayFrames)
                .Append("f</color>\n");
            if (_lastStalledPlayerIndex >= 0)
            {
                _debugSb.Append("  缺输入: F").Append(_lastStalledLogicFrame)
                    .Append(" P").Append(_lastStalledPlayerIndex).Append('\n');
            }
        }

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            var inp = GetDebugInput((byte)i);
            if (i == 0)
                _debugSb.Append("<color=#88ffff>");
            else
                _debugSb.Append("<color=#ffff88>");
            _debugSb.Append("P").Append(i).Append(" (F").Append(inp.frame).Append("): ");
            _debugSb.Append("H:").Append(inp.MoveHorizontal).Append(" V:").Append(inp.MoveVertical).Append(' ');
            _debugSb.Append(inp.Shoot ? "Z " : "· ");
            _debugSb.Append(inp.Bomb ? "X " : "· ");
            _debugSb.Append(inp.SlowMode ? "SLOW" : "FAST");
            _debugSb.Append("</color>\n");
        }

        _inputDebugHud.SetDebugText(_debugSb);
    }

    #endregion
}