using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomPanel : UIPanel
{
    [Header("UI References")]
    public TMP_Text roomInfoText;
    public Button startBattleBtn;
    public Button leaveRoomBtn;

    public GameObject playerInfoPrefab;
    public Transform playerInfoRoot;

    List<PlayerInfoItem> _playerItems = new();

    public override void Initialize()
    {
        startBattleBtn.onClick.AddListener(OnStartBattleClicked);
        leaveRoomBtn.onClick.AddListener(OnLeaveRoomClicked);

        startBattleBtn.interactable = false;
        leaveRoomBtn.interactable = false;
    }

    public override void OnShow(object data = null)
    {
        bool isHost = RoomManager.Instance != null && RoomManager.Instance.IsHost;
        startBattleBtn.gameObject.SetActive(isHost);

        RefreshUI();
        SetupEventListeners();
    }

    public override void OnHide()
    {
        RemoveEventListeners();
    }

    void OnDestroy()
    {
        RemoveEventListeners();
    }

    void SetupEventListeners()
    {
        RemoveEventListeners();

        var rm = RoomManager.Instance;
        if (rm == null)
            return;

        rm.OnRoomInfoUpdated += OnRoomInfoChanged;
        rm.OnDisconnectedFromHost += OnDisconnectedFromHost;
    }

    void RemoveEventListeners()
    {
        var rm = RoomManager.Instance;
        if (rm == null)
            return;

        rm.OnRoomInfoUpdated -= OnRoomInfoChanged;
        rm.OnDisconnectedFromHost -= OnDisconnectedFromHost;
    }

    void RefreshUI()
    {
        var rm = RoomManager.Instance;
        if (rm == null || !rm.IsInRoom)
        {
            roomInfoText.text = "未加入任何房间";
            startBattleBtn.interactable = false;
            leaveRoomBtn.interactable = false;
            ClearPlayerList();
            return;
        }

        var roomInfo = rm.CurrentRoom.Value;
        string roleLabel = rm.IsHost ? "房主" : $"玩家 {RoomManager.LocalPlayerIndex + 1}";
        roomInfoText.text =
            $"身份: {roleLabel}\n" +
            $"人数: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}\n" +
            $"地址: {roomInfo.IpAddress}:{roomInfo.Port}";

        UpdatePlayerList(roomInfo.PlayerCount, roomInfo.MaxPlayers);
        startBattleBtn.interactable = rm.IsHost && roomInfo.PlayerCount >= 2;
        leaveRoomBtn.interactable = true;
    }

    void UpdatePlayerList(int playerCount, int maxPlayers)
    {
        while (_playerItems.Count < maxPlayers)
        {
            var go = Instantiate(playerInfoPrefab, playerInfoRoot);
            var item = go.GetComponent<PlayerInfoItem>();
            if (item == null)
            {
                Debug.LogError("playerInfoPrefab missing PlayerInfoItem component!");
                Destroy(go);
                continue;
            }

            _playerItems.Add(item);
        }

        for (int i = maxPlayers; i < _playerItems.Count; i++)
            _playerItems[i].gameObject.SetActive(false);

        for (int i = 0; i < maxPlayers; i++)
        {
            var item = _playerItems[i];
            item.gameObject.SetActive(true);

            if (i < playerCount)
                item.SetOccupied(i, isHost: i == 0, isLocal: i == RoomManager.LocalPlayerIndex);
            else
                item.SetEmpty(i);
        }
    }

    void ClearPlayerList()
    {
        foreach (var item in _playerItems)
        {
            if (item != null)
                item.gameObject.SetActive(false);
        }
    }

    void OnStartBattleClicked()
    {
        RoomManager.Instance.EnterBattleScene();
        UIManager.Instance.ClosePanel<RoomPanel>();
    }

    void OnLeaveRoomClicked()
    {
        RoomManager.Instance.LeaveRoom();
        ReturnToMenuFromRoom();
    }

    void ReturnToMenuFromRoom()
    {
        var ui = UIManager.Instance;
        if (ui == null)
            return;

        ui.ClosePanel<RoomPanel>();
        ui.ShowPanelAsync<MenuPanel>().Forget();
    }

    void OnRoomInfoChanged(RoomInfo roomInfo)
    {
        RefreshUI();
    }

    void OnDisconnectedFromHost(string message)
    {
        Logger.Warn(message, LogTag.Room);
        ReturnToMenuFromRoom();
    }
}
