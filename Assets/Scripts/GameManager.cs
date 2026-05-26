using UnityEngine;

public class GameManager : SingletonMono<GameManager>
{  
    public static uint logicFPS = 60;

    protected override void OnSingletonInit()
    {
        // 同机多开联机测试时，窗口失焦也要持续跑 Update，
        // 否则网络收发与锁步推进会停摆，导致双方互相等待。
        Application.runInBackground = true;
        SetApplicationFPS(-1 , false);
    }

    public void SetApplicationFPS(int renderFPS, bool vsync)
    {
        QualitySettings.vSyncCount = vsync ? 1 : 0; // 关闭垂直同步
        Application.targetFrameRate = renderFPS;
    }
}
