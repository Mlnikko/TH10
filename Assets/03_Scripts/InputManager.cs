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

public class InputManager : SingletonMono<InputManager>
{
    const int MAX_PLAYERS = 4;

    InputKeyCodeConfig _inputKeyCodeCfg;
    Dictionary<uint, FrameInput>[] _inputFrames;
    FrameInput[] _currentConsumedInputs;
    bool _isInitialized = false;

    protected override void OnSingletonInit()
    {
        base.OnSingletonInit();
        InitializeForGame();
    }

    public void InitializeForGame()
    {
        _inputKeyCodeCfg = new InputKeyCodeConfig(); // 可根据需要加载自定义配置
        _inputFrames = new Dictionary<uint, FrameInput>[MAX_PLAYERS];
        _currentConsumedInputs = new FrameInput[MAX_PLAYERS];

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            _inputFrames[i] = new Dictionary<uint, FrameInput>();
            _currentConsumedInputs[i] = FrameInput.None;
        }

        _isInitialized = true;
    }

    public void ClearAllInputs()
    {
        if (!_isInitialized) return;

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            _inputFrames[i]?.Clear();
            _currentConsumedInputs[i] = FrameInput.None;
        }
    }

    protected override void OnSingletonDestroy()
    {
        base.OnSingletonDestroy();
        ClearAllInputs();
        _isInitialized = false;
    }

    // 本地输入记录
    public FrameInput RecordLocalInput(byte playerIndex, uint logicFrame)
    {
        if (!_isInitialized || playerIndex >= MAX_PLAYERS) return FrameInput.None;

        // 防止重复写入同一帧
        if (_inputFrames[playerIndex].ContainsKey(logicFrame))
        {
            //Logger.Warn($"Input for P{playerIndex} at frame {logicFrame} already recorded!", LogTag.Input);
            return FrameInput.None;
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

        _inputFrames[playerIndex][logicFrame] = input;

        return input;
    }

    public void BroadcastLocalInput(FrameInput input)
    {
        if (NetworkManager.Instance.NetworkRole == NetworkRole.Client)
        {
            // 客户端：发送给主机
            NetworkManager.Instance.SendToHost(new InputMSG { frameInput = input });
        }
        else if (NetworkManager.Instance.NetworkRole == NetworkRole.Host)
        {
            // 主机：自己就是权威，直接广播（无需先发给自己）
            NetworkManager.Instance.Broadcast(new InputMSG { frameInput = input });
        }
        else
        {
            // 单机模式：无需广播
            Logger.Warn("单机模式下无需广播本地输入", LogTag.Input);
        }
    }

    // 远程输入注入

    public void AddRemoteInput(FrameInput input)
    {
        if (!_isInitialized || input.playerIndex >= MAX_PLAYERS) return;

        // 允许覆盖（用于重传），但可选地验证一致性
        if (_inputFrames[input.playerIndex].TryGetValue(input.frame, out var existing))
        {
            // 可选：如果不同，记录 desync（开发期）
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!StructEquals(existing, input))
            {
                Debug.LogWarning($"[Input] P{input.playerIndex} F{input.frame} conflict! Existing vs New");
            }
#endif
        }

        _inputFrames[input.playerIndex][input.frame] = input;
    }

    // 辅助方法：比较两个 FrameInput 是否相等（避免装箱）
    static bool StructEquals(in FrameInput a, in FrameInput b)
    {
        return a.frame == b.frame &&
               a.playerIndex == b.playerIndex &&
               a.directionPacked == b.directionPacked &&
               a.buttons == b.buttons;
    }

    // ========================
    // 输入就绪检查
    // ========================

    public bool AreAllInputsReady(uint logicFrame, bool[] activePlayers)
    {
        if (!_isInitialized || activePlayers == null) return false;

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (activePlayers[i])
            {
                if (!_inputFrames[i].ContainsKey(logicFrame))
                {
                    //Logger.Warn($"[Input] Waiting for P{i} input at frame {logicFrame}");
                    return false;
                }
            }
        }

        return true;
    }

    // ========================
    // 逻辑帧输入获取
    // ========================

    public FrameInput GetInputForFrame(byte playerIndex, uint logicFrame)
    {

        if (!_isInitialized || playerIndex >= MAX_PLAYERS)
        {
            return FrameInput.None;
        }

        if (_inputFrames[playerIndex].TryGetValue(logicFrame, out var input))
        {
            _currentConsumedInputs[playerIndex] = input;
            return input;
        }

        _currentConsumedInputs[playerIndex] = FrameInput.None;

        return FrameInput.None;
    }

    public void CleanupOldFrames(uint currentFrame, int keepWindow = 10)
    {
        uint minFrame = (currentFrame > (uint)keepWindow) ? currentFrame - (uint)keepWindow : 0;

        for (int p = 0; p < MAX_PLAYERS; p++)
        {
            var frames = _inputFrames[p];
            var keysToRemove = new List<uint>();

            foreach (var frame in frames.Keys)
            {
                if (frame < minFrame)
                    keysToRemove.Add(frame);
            }

            foreach (var frame in keysToRemove)
            {
                frames.Remove(frame);
            }
        }
    }

    #region 调试显示

    public FrameInput GetDebugInput(byte playerIndex)
    {
        if (!_isInitialized || playerIndex >= MAX_PLAYERS)
            return FrameInput.None;

        return _currentConsumedInputs[playerIndex];
    }

    [SerializeField] bool _showDebugInput = true;
    GUIStyle _debugStyle;

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
            string status = $"P{i} (F{inp.frame}): ";
            status += $"H:{inp.MoveHorizontal} V:{inp.MoveVertical} ";
            status += inp.Shoot ? "Z " : "· ";
            status += inp.Bomb ? "X " : "· ";
            status += inp.SlowMode ? "SLOW" : "FAST";

            GUI.color = (i == 0) ? Color.cyan : Color.yellow;
            GUILayout.Label(status, DebugStyle);
        }

        GUI.color = Color.white;
        GUILayout.EndArea();
    }

    #endregion
}