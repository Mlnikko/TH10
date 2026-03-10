using System;
using System.Collections.Generic;
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

// 移除不必要的引用，如 System.Collections.Generic (如果不再需要 Dictionary)

public class InputManager : SingletonMono<InputManager>
{
    const int MAX_PLAYERS = 4;
    // 【关键优化 1】环形缓冲区大小
    // 假设最大网络延迟 + 重传窗口为 2 秒 (120 帧 @ 60fps)。
    // 即使延迟达到 1 秒，只要 buffer > 60，旧数据被覆盖前肯定已经被消费了。
    // 设为 256 (2 的幂) 可以让编译器优化 % 运算为位运算 (& 255)，性能极致。
    const int BUFFER_SIZE = 256;
    const int BUFFER_MASK = BUFFER_SIZE - 1; // 用于快速取模

    InputKeyCodeConfig _inputKeyCodeCfg;

    // 【关键优化 2】改用二维数组代替 Dictionary 数组
    // _inputFrames[playerIndex][frame % BUFFER_SIZE]
    private FrameInput[][] _inputFrames;

    // 记录每个玩家当前已收到的最大帧号，用于快速判断是否就绪
    private uint[] _latestReceivedFrame;

    FrameInput[] _currentConsumedInputs;
    bool _isInitialized = false;

    protected override void OnSingletonInit()
    {
        base.OnSingletonInit();
        InitializeForGame();
    }

    public void InitializeForGame()
    {
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
            for (int j = 0; j < BUFFER_SIZE; j++) _inputFrames[i][j] = FrameInput.None;

            _latestReceivedFrame[i] = 0;
            _currentConsumedInputs[i] = FrameInput.None;
        }

        _isInitialized = true;
    }

    public void ClearAllInputs()
    {
        if (!_isInitialized) return;
        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            // 只需重置最大帧号，并将缓冲区标记为无效（可选，因为逻辑依赖帧号判断）
            _latestReceivedFrame[i] = 0;
            // 如果需要彻底清空数据以防读取脏数据：
            // Array.Fill(_inputFrames[i], FrameInput.None); 
        }
    }

    // --- 核心逻辑修改 ---

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

        var input = FrameInput.Create(
            logicFrame,
            playerIndex,
            (sbyte)(Input.GetKey(_inputKeyCodeCfg.moveRight) ? 1 : Input.GetKey(_inputKeyCodeCfg.moveLeft) ? -1 : 0),
            (sbyte)(Input.GetKey(_inputKeyCodeCfg.moveUp) ? 1 : Input.GetKey(_inputKeyCodeCfg.moveDown) ? -1 : 0),
            Input.GetKey(_inputKeyCodeCfg.shoot),
            Input.GetKey(_inputKeyCodeCfg.bomb),
            Input.GetKey(_inputKeyCodeCfg.slow),
            Input.anyKey
        );

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

    public bool AreAllInputsReady(uint logicFrame, bool[] activePlayers)
    {
        if (!_isInitialized || activePlayers == null) return false;

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (activePlayers[i])
            {
                // 【优化】直接比较最大帧号，O(1) 复杂度，无哈希查找
                if (_latestReceivedFrame[i] < logicFrame)
                {
                    return false;
                }

                // 双重保险：防止环形缓冲覆盖导致读取到错误的旧帧 (虽然逻辑帧号检查通常足够)
                // 如果逻辑帧号比最新帧小很多，可能已经被覆盖了？
                // 只要 BUFFER_SIZE > 最大延迟帧数，这里取到的 frame 一定匹配 logicFrame
                int index = (int)(logicFrame & BUFFER_MASK);
                if (_inputFrames[i][index].frame != logicFrame)
                {
                    // 这种情况说明帧号回绕了且新数据还没到，或者数据丢失
                    return false;
                }
            }
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

    // --- 【关键优化 3】彻底删除 CleanupOldFrames ---
    // 不再需要！环形缓冲自动管理内存，旧数据直接被新数据覆盖。
    // public void CleanupOldFrames(...) { ... } <--- 删掉它！

    #region 调试显示优化

    public FrameInput GetDebugInput(byte playerIndex)
    {
        if (!_isInitialized || playerIndex >= MAX_PLAYERS) return FrameInput.None;
        return _currentConsumedInputs[playerIndex];
    }

    [SerializeField] bool _showDebugInput = true;
    GUIStyle _debugStyle;
    // 复用 StringBuilder 用于调试，避免 OnGUI 分配
    System.Text.StringBuilder _debugSb = new(128);

    GUIStyle DebugStyle
    {
        get
        {
            _debugStyle ??= new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    normal = { textColor = Color.white },
                    alignment = TextAnchor.UpperLeft
                };
            return _debugStyle;
        }
    }

    void OnGUI()
    {
        if (!_showDebugInput || !_isInitialized) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 200));
        GUILayout.Space(8);

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            var inp = GetDebugInput((byte)i);

            // 【优化】使用 StringBuilder 复用，避免每帧生成字符串
            _debugSb.Clear();
            _debugSb.Append("P").Append(i).Append(" (F").Append(inp.frame).Append("): ");
            _debugSb.Append("H:").Append(inp.MoveHorizontal).Append(" V:").Append(inp.MoveVertical).Append(" ");
            _debugSb.Append(inp.Shoot ? "Z " : "· ");
            _debugSb.Append(inp.Bomb ? "X " : "· ");
            _debugSb.Append(inp.SlowMode ? "SLOW" : "FAST");

            GUI.color = (i == 0) ? Color.cyan : Color.yellow;
            GUILayout.Label(_debugSb.ToString(), DebugStyle);
        }

        GUI.color = Color.white;
        GUILayout.EndArea();
    }

    #endregion
}