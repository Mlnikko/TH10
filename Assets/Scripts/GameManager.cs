using UnityEngine;

public enum E_FPSMode
{
    NoLimit,
    PC60,
    PC120
}

public class GameManager : SingletonMono<GameManager>
{
    protected override void OnSingletonInit()
    {
        SetApplicationFPS(E_FPSMode.NoLimit, false);
    }

    public void SetApplicationFPS(E_FPSMode fPSMode, bool vsync)
    {
        QualitySettings.vSyncCount = vsync ? 1 : 0; // 关闭垂直同步
        switch (fPSMode)
        {
            case E_FPSMode.NoLimit:
                Application.targetFrameRate = -1;
                break;
            case E_FPSMode.PC60:
                Application.targetFrameRate = 60;
                break;
            case E_FPSMode.PC120:
                Application.targetFrameRate = 120;
                break;
        }
    }
}
