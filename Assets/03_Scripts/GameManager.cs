using System;
using System.Collections;
using UnityEngine;

public enum E_FPSMode
{
    Default,
    PC60,
    PC120
}

public class GameManager : SingletonMono<GameManager>
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
            OnUpdateFPS?.Invoke(fps);
        }
    }
    #endregion
    public event Action<float> OnUpdateFPS;

    protected override void OnSingletonInit()
    {
        GameLogger.AddHandler(new ConsoleHandler());
        LoadResource();      
        SetApplicationFPS(E_FPSMode.Default);     
    }

    void Start()
    {
        EnterTitle(true);
    }

    void LoadResource()
    {
        UIManager.Instance.LoadPrefabRefrence();
    }

    void Update()
    {
        UpdateDebugInfo();
    }

    public void SetApplicationFPS(E_FPSMode fPSMode)
    {
        QualitySettings.vSyncCount = 0; // 关闭垂直同步
        switch (fPSMode)
        {
            case E_FPSMode.Default:
                Application.targetFrameRate = 60;
                break;
            case E_FPSMode.PC60:
                Application.targetFrameRate = 60;
                break;
            case E_FPSMode.PC120:
                Application.targetFrameRate = 120;
                break;
        }  
    }

    IEnumerator FirstEnterTitle()
    {
        BasePanel loadingPanel = UIManager.Instance.OpenPanel(E_Panel.Loading);
        yield return new WaitForSeconds(3);
        AudioManager.Instance.PlayAudio(E_AudioName.Title);
        loadingPanel.PanelFadeOut(1);
        yield return new WaitForSeconds(1);
        UIManager.Instance.OpenPanel(E_Panel.Title);
    }

    void EnterTitle(bool isFirst)
    {
        if (isFirst)
        {
            StartCoroutine(FirstEnterTitle());
        }
        else
        {

        }
    }
}
