using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct FrameInput
{
    public ushort frame;
    public byte playerIndex;

    public sbyte moveHorizontal; // -1, 0, +1
    public sbyte moveVertical;   // -1, 0, +1
    public bool shoot;           // Z
    public bool bomb;            // X
    public bool slowMode;        // LeftShift
    public bool anyKey;

    public static FrameInput Default => new() { frame = 0, playerIndex = 0 };
}

public class InputManager : SingletonMono<InputManager>
{
    private const int MAX_PLAYERS = 4;

    private Dictionary<ushort, FrameInput>[] _inputFrames;
    private FrameInput[] _currentConsumedInputs;
    private bool _isInitialized = false;

    protected override void OnSingletonInit()
    {
        base.OnSingletonInit();
        InitializeForGame();
    }

    // ========================
    // 初始化与清理
    // ========================

    public void InitializeForGame()
    {
        _inputFrames = new Dictionary<ushort, FrameInput>[MAX_PLAYERS];
        _currentConsumedInputs = new FrameInput[MAX_PLAYERS];

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            _inputFrames[i] = new Dictionary<ushort, FrameInput>();
            _currentConsumedInputs[i] = FrameInput.Default;
        }

        _isInitialized = true;
    }

    public void ClearAllInputs()
    {
        if (!_isInitialized) return;

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            _inputFrames[i]?.Clear();
            _currentConsumedInputs[i] = FrameInput.Default;
        }
    }

    protected override void OnSingletonDestroy()
    {
        base.OnSingletonDestroy();
        ClearAllInputs();
        _isInitialized = false;
    }

    // ========================
    // 输入记录（本地）
    // ========================

    public void RecordLocalInput(byte playerId, ushort logicFrame)
    {
        if (!_isInitialized || playerId >= MAX_PLAYERS) return;

        // 防止重复写入同一帧（安全防护）
        if (_inputFrames[playerId].ContainsKey(logicFrame))
        {
            Debug.LogWarning($"Input for P{playerId} at frame {logicFrame} already recorded!");
            return;
        }

        var input = new FrameInput
        {
            frame = logicFrame,
            playerIndex = playerId,
            moveHorizontal = (sbyte)(Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0),
            moveVertical = (sbyte)(Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0),
            shoot = Input.GetKey(KeyCode.Z),
            bomb = Input.GetKey(KeyCode.X),
            slowMode = Input.GetKey(KeyCode.LeftShift),
            anyKey = Input.anyKey
        };

        _inputFrames[playerId][logicFrame] = input;

        // TODO: NetworkManager?.SendInput(input);
    }

    // ========================
    // 输入注入（远程）
    // ========================

    public void AddRemoteInput(in FrameInput input)
    {
        if (!_isInitialized || input.playerIndex >= MAX_PLAYERS) return;

        // 同样防止覆盖（可选：允许覆盖用于重传）
        if (_inputFrames[input.playerIndex].ContainsKey(input.frame))
        {
            // 可选：验证是否相同，不同则报错（防作弊/desync）
            // Debug.LogWarning($"Remote input for P{input.playerIndex} F{input.frame} already exists!");
            return;
        }

        _inputFrames[input.playerIndex][input.frame] = input;
    }

    // ========================
    // 输入就绪检查
    // ========================

    public bool AreAllInputsReady(ushort logicFrame, BitArray activePlayers)
    {
        if (!_isInitialized || activePlayers == null || activePlayers.Length != MAX_PLAYERS)
            return false;

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            if (activePlayers[i])
            {
                if (!_inputFrames[i].ContainsKey(logicFrame))
                    return false;
            }
        }
        return true;
    }

    // ========================
    // 逻辑帧输入获取（供 PlayerControlSystem 使用）
    // ========================

    public bool TryGetInputForFrame(byte playerId, ushort logicFrame, out FrameInput input)
    {
        if (!_isInitialized || playerId >= MAX_PLAYERS)
        {
            input = FrameInput.Default;
            return false;
        }

        if (_inputFrames[playerId].TryGetValue(logicFrame, out input))
        {
            _currentConsumedInputs[playerId] = input; // ✅ 用于调试
            return true;
        }

        input = FrameInput.Default;
        _currentConsumedInputs[playerId] = input;
        return false;
    }

    // ========================
    // 调试支持
    // ========================

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    public FrameInput GetDebugInput(byte playerId)
    {
        if (!_isInitialized || playerId >= MAX_PLAYERS)
            return FrameInput.Default;

        return _currentConsumedInputs[playerId];
    }

    private bool _showDebugInput = true;
    private GUIStyle _debugStyle;

    private GUIStyle DebugStyle
    {
        get
        {
            if (_debugStyle == null)
            {
                _debugStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    normal = { textColor = Color.white },
                    alignment = TextAnchor.UpperLeft
                };
            }
            return _debugStyle;
        }
    }

    private void OnGUI()
    {
        if (!_showDebugInput || !_isInitialized) return;

        GUILayout.BeginArea(new Rect(10, 10, 350, 200));
        GUILayout.Label($"Logic Frame: {GameTimeManager.CurrentLogicFrame}", DebugStyle);
        GUILayout.Space(8);

        for (int i = 0; i < MAX_PLAYERS; i++)
        {
            var inp = GetDebugInput((byte)i);
            string status = $"P{i} (F{inp.frame}): ";
            status += $"H:{inp.moveHorizontal} V:{inp.moveVertical} ";
            status += inp.shoot ? "Z " : "· ";
            status += inp.bomb ? "X " : "· ";
            status += inp.slowMode ? "SLOW" : "FAST";

            GUI.color = (i == 0) ? Color.cyan : Color.yellow;
            GUILayout.Label(status, DebugStyle);
        }

        GUI.color = Color.white;
        GUILayout.EndArea();
    }
#endif
}