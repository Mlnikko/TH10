using System;
using UnityEngine;

public enum GameState
{
    Start,
    Loading,
    Select,
    Gameing,
    End
}

public enum SceneName
{
    Title,
    Stage1,
    Stage2,
    Stage3,
    Stage4,
    Stage5,
    Stage6,
    Extra,
}

public enum FPSMode
{
    Default,
    PC60,
    PC120
}

public class GameManager : MonoSingleton<GameManager>
{
    #region fps计算
    float _fpsCalculationInterval = 0.5f;
    int _frameCount;
    float _timer;
    void UpdateDebugInfo()
    {
        // 累计帧数和时间
        _frameCount++;
        _timer += Time.deltaTime;

        // 定期计算平均帧率
        if (_timer >= _fpsCalculationInterval)
        {
            float fps = _frameCount / _timer;
            _frameCount = 0;
            _timer = 0f;

            // 安全触发事件
            UpdateFPS?.Invoke(fps);
        }
    }
    #endregion

    GameState state;
    public event Action<float> UpdateFPS;

    protected override void OnSingletonInit()
    {
        SetApplicationFPS(FPSMode.Default);
        SetGameState(GameState.Start);
        LoadScene(SceneName.Title);
    }  

    void Update()
    {
        UpdateDebugInfo();
    }
    public void SetGameState(GameState state)
    {
        switch (state)
        {
            case GameState.Start:
                break;
            case GameState.Loading:
                break;
            case GameState.Select:
                break;
            case GameState.Gameing:
                break;
            case GameState.End:
                break;
        }
    }
    public void SetApplicationFPS(FPSMode fPSMode)
    {
        QualitySettings.vSyncCount = 0; // 关闭垂直同步
        switch (fPSMode)
        {
            case FPSMode.Default:
                Application.targetFrameRate = 60;
                break;
            case FPSMode.PC60:
                Application.targetFrameRate = 60;
                break;
            case FPSMode.PC120:
                Application.targetFrameRate = 120;
                break;
        }  
    }

    public void LoadScene(SceneName sceneName)
    {
        switch (sceneName)
        {
            case SceneName.Title:
                break;
            case SceneName.Stage1:
                break;
            case SceneName.Stage2:
                break;
            case SceneName.Stage3:
                break;
            case SceneName.Stage4:
                break;
            case SceneName.Stage5:
                break;
            case SceneName.Stage6:
                break;
            case SceneName.Extra:
                break;
        }
    }
}
