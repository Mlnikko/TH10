// MenuPanel.cs
using UnityEngine;
using UnityEngine.UI;

public class MenuPanel : UIPanel
{
    public Button localModeBtn;
    public Button onlineModeBtn;
    public Button settingBtn;
    public Button replayBtn;
    public Button quitGameBtn;

    public override void OnShow(object data = null)
    {
        base.OnShow(data);
        localModeBtn.onClick.AddListener(OnLocalModeClicked);
        onlineModeBtn.onClick.AddListener(OnOnlineModeClicked);
        settingBtn.onClick.AddListener(OnSettingClicked);
        replayBtn.onClick.AddListener(OnReplayClicked);
        quitGameBtn.onClick.AddListener(OnQuitGameClicked);
    }


    void OnLocalModeClicked()
    {
        UIManager.Instance.CloseAll();
        SceneLoader.LoadScene("BattleScene");
    }

    void OnOnlineModeClicked()
    {
        // 弹出二级选择窗口
        UIManager.Instance.ShowPanelAsync<OnlineModePanel>().Forget();
    }

    void OnSettingClicked()
    {
        UIManager.Instance.ShowPanelAsync<SettingsPanel>().Forget();
    }

    void OnReplayClicked()
    {
        // TODO: 回放系统
    }

    void OnQuitGameClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public override void OnHide()
    {
        base.OnHide();
        localModeBtn.onClick.RemoveListener(OnLocalModeClicked);
        onlineModeBtn.onClick.RemoveListener(OnOnlineModeClicked);
        settingBtn.onClick.RemoveListener(OnSettingClicked);
        replayBtn.onClick.RemoveListener(OnReplayClicked);
        quitGameBtn.onClick.RemoveListener(OnQuitGameClicked);
    }
}