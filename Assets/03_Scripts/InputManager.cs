using System;
using UnityEngine;

public class InputManager : SingletonMono<InputManager>
{
    #region  ‰»Î◊Ë»˚øÿ÷∆
    public static bool IsInputBlocked { get; private set; }
    public static void BlockInput() => IsInputBlocked = true;
    public static void ReleaseInput() => IsInputBlocked = false;
    #endregion

    #region  ‰»Îª∫¥Ê
    bool esc_KeyInputDown;

    bool leftShift_KeyInputDown;
    bool leftShift_KeyInputUp;

    bool z_KeyInputDown;
    bool z_KeyInputUp;

    bool x_KeyInputDown;

    bool up_KeyInputDown;
    bool up_KeyInputUp;

    bool down_KeyInputDown;
    bool down_KeyInputUp;

    bool right_KeyInputDown;
    bool right_KeyInputUp;

    bool left_KeyInputDown;
    bool left_KeyInputUp;
    bool any_KeyInput
    {
        get
        {
            return esc_KeyInputDown | leftShift_KeyInputDown | z_KeyInputDown | x_KeyInputDown | up_KeyInputDown | down_KeyInputDown | left_KeyInputDown | right_KeyInputDown;
        }
    }
    #endregion

    public event Action<bool> OnKeyInput_LS;
    public event Action<bool> OnKeyInput_Z;
    public event Action OnKeyInput_X;

    public event Action<bool> OnKeyInput_Up;
    public event Action<bool> OnKeyInput_Down;
    public event Action<bool> OnKeyInput_Left;
    public event Action<bool> OnKeyInput_Right;

    public event Action OnKeyInput_Esc;
    public event Action OnKeyInput_Any;
    public float HorizontalAxisRaw
    {
        get
        {
            return Input.GetAxisRaw("Horizontal");
        }
    }
    public float VerticalAxisRaw
    {
        get
        {
            return Input.GetAxisRaw("Vertical");
        }
    }

    protected override void OnSingletonInit()
    {
        base.OnSingletonInit();
        ReleaseInput();
    }

    void Update()
    {
        GetInput();
        HandleInput();
    }
    void GetInput()
    {
        if (IsInputBlocked) return;

        esc_KeyInputDown = Input.GetKeyDown(KeyCode.Escape);

        leftShift_KeyInputDown = Input.GetKeyDown(KeyCode.LeftShift);
        leftShift_KeyInputUp = Input.GetKeyUp(KeyCode.LeftShift);

        z_KeyInputDown = Input.GetKeyDown(KeyCode.Z);
        z_KeyInputUp = Input.GetKeyUp(KeyCode.Z);

        x_KeyInputDown = Input.GetKeyDown(KeyCode.X);

        up_KeyInputDown = Input.GetKeyDown(KeyCode.UpArrow);
        up_KeyInputUp = Input.GetKeyUp(KeyCode.UpArrow);

        down_KeyInputDown = Input.GetKeyDown(KeyCode.DownArrow);
        down_KeyInputUp = Input.GetKeyUp(KeyCode.DownArrow);

        left_KeyInputDown = Input.GetKeyDown(KeyCode.LeftArrow);
        left_KeyInputUp = Input.GetKeyUp(KeyCode.LeftArrow);

        right_KeyInputDown = Input.GetKeyDown(KeyCode.RightArrow);
        right_KeyInputUp = Input.GetKeyUp(KeyCode.RightArrow);
    }
    void HandleInput()
    {
        if (leftShift_KeyInputDown) OnKeyInput_LS?.Invoke(true);
        if (leftShift_KeyInputUp) OnKeyInput_LS?.Invoke(false);

        if (z_KeyInputDown) OnKeyInput_Z?.Invoke(true);
        if (z_KeyInputUp) OnKeyInput_Z?.Invoke(false);

        if (x_KeyInputDown) OnKeyInput_X?.Invoke();

        if (up_KeyInputDown) OnKeyInput_Up?.Invoke(true);
        if (up_KeyInputUp) OnKeyInput_Up?.Invoke(false);

        if (down_KeyInputDown) OnKeyInput_Down?.Invoke(true);
        if (down_KeyInputUp) OnKeyInput_Down?.Invoke(false);

        if (left_KeyInputDown) OnKeyInput_Left?.Invoke(true);
        if (left_KeyInputUp) OnKeyInput_Left?.Invoke(false);

        if (right_KeyInputDown) OnKeyInput_Right?.Invoke(true);
        if (right_KeyInputUp) OnKeyInput_Right?.Invoke(false);

        if (esc_KeyInputDown) OnKeyInput_Esc?.Invoke();
        if (any_KeyInput) OnKeyInput_Any?.Invoke();
    }
}
