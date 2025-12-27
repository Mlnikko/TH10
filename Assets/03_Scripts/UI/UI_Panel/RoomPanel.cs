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
        if(!RoomManager.Instance.IsHost)
        {
            startBattleBtn.gameObject.SetActive(false);
        }
        RefreshUI();
        SetupEventListeners();
    }

    public override void OnHide()
    {
        RemoveEventListeners();
    }

    void SetupEventListeners()
    {
        var rm = RoomManager.Instance;     
        rm.OnRoomInfoUpdated += OnRoomInfoChanged;
    }

    void RemoveEventListeners()
    {
        var rm = RoomManager.Instance;
        if (rm == null) return;
        rm.OnRoomInfoUpdated -= OnRoomInfoChanged;
    }

    void RefreshUI()
    {
        var rm = RoomManager.Instance;

        if (!rm.IsInRoom)
        {
            roomInfoText.text = "未加入任何房间";
            startBattleBtn.interactable = false;
            leaveRoomBtn.interactable = false;
            ClearPlayerList();
            return;
        }

        var roomInfo = rm.CurrentRoom.Value;
        roomInfoText.text =
            $"房间 ID: {roomInfo.RoomId}\n" +
            $"主机: {roomInfo.HostName}\n" +
            $"人数: {roomInfo.PlayerCount}/{roomInfo.MaxPlayers}\n" +
            $"IP: {roomInfo.IpAddress}";

        // 刷新玩家列表
        UpdatePlayerList(roomInfo.PlayerCount, roomInfo.MaxPlayers, roomInfo.HostName);

        // 只有房主且至少2人才能开始
        startBattleBtn.interactable = rm.IsHost && roomInfo.PlayerCount >= 2;
        leaveRoomBtn.interactable = true;
    }

    void UpdatePlayerList(int playerCount, int maxPlayers, string hostName)
    {
        // 确保预制体数量匹配 maxPlayers
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

        // 超出部分隐藏（通常不会发生）
        for (int i = maxPlayers; i < _playerItems.Count; i++)
        {
            _playerItems[i].gameObject.SetActive(false);
        }

        // 更新每个槽位
        for (int i = 0; i < maxPlayers; i++)
        {
            var item = _playerItems[i];
            item.gameObject.SetActive(true);

            if (i == 0)
            {
                // 槽位 0 固定为房主
                item.SetInfo(hostName, isHost: true);
            }
            else if (i < playerCount)
            {
                // 其他已加入玩家（简化命名）
                item.SetInfo($"Player {i + 1}", isHost: false);
            }
            else
            {
                // 空槽位
                item.SetEmpty(i);
            }
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

    // ====== 按钮回调 ======
    void OnStartBattleClicked()
    {
        RoomManager.Instance.EnterBattleScene();
        UIManager.Instance.ClosePanel<RoomPanel>();
    }

    void OnLeaveRoomClicked()
    {
        RoomManager.Instance.LeaveRoom();
        UIManager.Instance.GoBack();
    }

    // ====== 事件响应 ======
    void OnLocalLeaveRoom()
    {
        UIManager.Instance.GoBack();
    }

    void OnRoomInfoChanged(RoomInfo roomInfo)
    {
        RefreshUI();
    }
}