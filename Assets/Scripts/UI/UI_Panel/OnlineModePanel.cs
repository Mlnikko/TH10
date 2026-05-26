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
        RoomManager.Instance.CreateRoom(maxPlayers: 4);

        UIManager.Instance.ClosePanel<OnlineModePanel>();
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