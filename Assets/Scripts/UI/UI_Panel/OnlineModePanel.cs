using UnityEngine;
using UnityEngine.UI;

public class OnlineModePanel : UIPanel
{
    public Button createRoomBtn;
    public Button joinRoomBtn;
    public Button cancelButton;

    public override void OnShow(object data = null)
    {
        base.OnShow(data);
        createRoomBtn.onClick.AddListener(OnCreateRoomClicked);
        joinRoomBtn.onClick.AddListener(OnJoinRoomClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
    }

    void OnCreateRoomClicked()
    {
        // 自动创建房间（使用默认名称）
        string hostName = "Player" + (Random.Range(10, 99)); // 简化命名
        RoomManager.Instance.CreateRoom(hostName, maxPlayers: 4);

        // 进入房间界面
        UIManager.Instance.ShowPanelAsync<RoomPanel>().Forget();
    }

    void OnJoinRoomClicked()
    {
        UIManager.Instance.ShowPanelAsync<JoinRoomInputPanel>().Forget();
    }

    void OnCancelClicked()
    {
        UIManager.Instance.GoBack();
    }

    public override void OnHide()
    {
        base.OnHide();
        createRoomBtn.onClick.RemoveListener(OnCreateRoomClicked);
        joinRoomBtn.onClick.RemoveListener(OnJoinRoomClicked);
        cancelButton.onClick.RemoveListener(OnCancelClicked);
    }
}