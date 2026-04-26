using UnityEngine;

public class GameManager : SingletonMono<GameManager>
{  
    public static uint logicFPS = 60;

    protected override void OnSingletonInit()
    {
        SetApplicationFPS(-1 , false);
    }

    public void SetApplicationFPS(int renderFPS, bool vsync)
    {
        QualitySettings.vSyncCount = vsync ? 1 : 0; // 关闭垂直同步
        Application.targetFrameRate = renderFPS;
    }
}
