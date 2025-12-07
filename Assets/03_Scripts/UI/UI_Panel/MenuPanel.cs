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

    public override void Initialize()
    {
        localModeBtn.onClick.AddListener(OnLocalModeClicked);
        onlineModeBtn.onClick.AddListener(OnOnlineModeClicked);
        settingBtn.onClick.AddListener(OnSettingClicked);
        replayBtn.onClick.AddListener(OnReplayClicked);
        quitGameBtn.onClick.AddListener(OnQuitGameClicked);
    }

    private void OnLocalModeClicked()
    {
        // TODO: 单人模式
        SceneLoader.LoadScene("BattleScene");
        //GameState.SetIsLocalMode(true);
    }

    private void OnOnlineModeClicked()
    {
        // 弹出二级选择窗口
        UIManager.Instance.ShowPanel<OnlineModePopup>();
    }

    private void OnSettingClicked()
    {
        UIManager.Instance.ShowPanel<SettingPanel>();
    }

    private void OnReplayClicked()
    {
        // TODO: 回放系统
    }

    private void OnQuitGameClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}