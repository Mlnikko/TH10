// OnlineModePopup.cs
using UnityEngine;
using UnityEngine.UI;

public class OnlineModePopup : UIPanel
{
    public Button createRoomBtn;
    public Button joinRoomBtn;
    public Button cancelButton;

    public override void Initialize()
    {
        createRoomBtn.onClick.AddListener(OnCreateRoomClicked);
        joinRoomBtn.onClick.AddListener(OnJoinRoomClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void OnCreateRoomClicked()
    {
        // 关闭弹窗
        UIManager.Instance.HidePanel<OnlineModePopup>();

        // 自动创建房间（使用默认名称）
        string hostName = "Player" + (Random.Range(10, 99)); // 简化命名
        RoomManager.Instance.CreateRoom(hostName, maxPlayers: 4);

        // 进入房间界面
        UIManager.Instance.ShowPanel<RoomPanel>();
    }

    private void OnJoinRoomClicked()
    {
        // 关闭当前弹窗，打开输入面板
        UIManager.Instance.HidePanel<OnlineModePopup>();
        UIManager.Instance.ShowPanel<JoinRoomInputPanel>();
    }

    private void OnCancelClicked()
    {
        UIManager.Instance.HidePanel<OnlineModePopup>();
    }
}