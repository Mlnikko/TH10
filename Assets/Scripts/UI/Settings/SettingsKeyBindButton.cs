using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum E_InputBindingSlot
{
    MoveLeft,
    MoveRight,
    MoveUp,
    MoveDown,
    Shoot,
    Bomb,
    Slow,
    Pause,
}

/// <summary>单条键位重绑控件：点击后等待下一按键。</summary>
public class SettingsKeyBindButton : MonoBehaviour
{
    [SerializeField] E_InputBindingSlot slot;
    [SerializeField] TMP_Text labelText;
    [SerializeField] Button bindButton;
    [SerializeField] TMP_Text keyText;

    bool _listening;
    Action _onRebind;

    public void Setup(Action onRebind)
    {
        _onRebind = onRebind;
        if (bindButton != null)
        {
            bindButton.onClick.RemoveListener(BeginListen);
            bindButton.onClick.AddListener(BeginListen);
        }
        RefreshDisplay();
    }

    void OnDisable()
    {
        _listening = false;
    }

    void Update()
    {
        if (!_listening) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _listening = false;
            RefreshDisplay();
            return;
        }

        foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
        {
            if (key == KeyCode.None) continue;
            if (!Input.GetKeyDown(key)) continue;

            if (TryAssignKey(key))
            {
                _listening = false;
                RefreshDisplay();
                _onRebind?.Invoke();
            }
            return;
        }
    }

    public void RefreshDisplay()
    {
        if (labelText != null)
            labelText.text = GetSlotDisplayName(slot);

        if (keyText != null)
            keyText.text = _listening ? "按下按键…" : GetKeyDisplayName(ReadKeyFromService());
    }

    void BeginListen()
    {
        _listening = true;
        RefreshDisplay();
    }

    bool TryAssignKey(KeyCode key)
    {
        var data = GameSettingsService.Instance.Data;
        if (data?.keyBindings == null) return false;

        ref var cfg = ref GetKeyRef(data.keyBindings, slot);
        if (IsKeyUsedByOtherSlot(data.keyBindings, slot, key))
        {
            Logger.Warn($"键位 {key} 已被其他操作占用。", LogTag.UI);
            return false;
        }

        cfg = key;
        return true;
    }

    KeyCode ReadKeyFromService()
    {
        var bindings = GameSettingsService.Instance.Data?.keyBindings;
        if (bindings == null) return KeyCode.None;
        return GetKeyRef(bindings, slot);
    }

    static ref KeyCode GetKeyRef(InputKeyCodeConfig cfg, E_InputBindingSlot s)
    {
        switch (s)
        {
            case E_InputBindingSlot.MoveLeft: return ref cfg.moveLeft;
            case E_InputBindingSlot.MoveRight: return ref cfg.moveRight;
            case E_InputBindingSlot.MoveUp: return ref cfg.moveUp;
            case E_InputBindingSlot.MoveDown: return ref cfg.moveDown;
            case E_InputBindingSlot.Shoot: return ref cfg.shoot;
            case E_InputBindingSlot.Bomb: return ref cfg.bomb;
            case E_InputBindingSlot.Slow: return ref cfg.slow;
            case E_InputBindingSlot.Pause: return ref cfg.pause;
            default: return ref cfg.pause;
        }
    }

    static bool IsKeyUsedByOtherSlot(InputKeyCodeConfig cfg, E_InputBindingSlot self, KeyCode key)
    {
        foreach (E_InputBindingSlot s in Enum.GetValues(typeof(E_InputBindingSlot)))
        {
            if (s == self) continue;
            if (GetKeyRef(cfg, s) == key) return true;
        }
        return false;
    }

    static string GetSlotDisplayName(E_InputBindingSlot s)
    {
        return s switch
        {
            E_InputBindingSlot.MoveLeft => "向左",
            E_InputBindingSlot.MoveRight => "向右",
            E_InputBindingSlot.MoveUp => "向上",
            E_InputBindingSlot.MoveDown => "向下",
            E_InputBindingSlot.Shoot => "射击",
            E_InputBindingSlot.Bomb => "炸弹",
            E_InputBindingSlot.Slow => "低速",
            E_InputBindingSlot.Pause => "暂停",
            _ => s.ToString(),
        };
    }

    static string GetKeyDisplayName(KeyCode key)
    {
        if (key == KeyCode.None) return "未绑定";
        return key switch
        {
            KeyCode.LeftArrow => "←",
            KeyCode.RightArrow => "→",
            KeyCode.UpArrow => "↑",
            KeyCode.DownArrow => "↓",
            KeyCode.LeftShift => "L-Shift",
            KeyCode.RightShift => "R-Shift",
            _ => key.ToString(),
        };
    }
}
