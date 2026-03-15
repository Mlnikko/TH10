using System;
using Unity.Networking.Transport;

[Serializable]
public struct RoomInfo
{
    public int RoomId;
    public string HostName;
    public string IpAddress;
    public ushort Port;

    public byte PlayerCount;
    public byte MaxPlayers;

    public override readonly string ToString()
    {
        return $"[{RoomId}] {HostName} ({PlayerCount}/{MaxPlayers}) @ {IpAddress}:{Port}";
    }
}

public class RoomManager : SingletonMono<RoomManager>
{
    // ====== 事件（UI 用）======
    //public event Action OnRoomCreated;
    public event Action<RoomInfo> OnRoomInfoUpdated; // 房间信息更新事件

    // ====== 状态 ======
    public RoomInfo? CurrentRoom { get; private set; }
    public bool IsInRoom => CurrentRoom.HasValue;
    public bool IsHost => NetworkManager.Instance.NetworkRole == NetworkRole.Host;
    public int PlayerCount => CurrentRoom?.PlayerCount ?? 0;

    public static byte LocalPlayerIndex;

    // ====== 房间操作 ======

    public void CreateRoom(string hostName, ushort port = 7777, byte maxPlayers = 4)
    {
        if (IsInRoom) LeaveRoom();

        // 获取本机局域网 IP
        string localIP = NetworkTool.GetLocalIPAddress();

        CurrentRoom = new RoomInfo
        {
            RoomId = UnityEngine.Random.Range(10000, 99999),
            HostName = hostName,
            PlayerCount = 1,
            MaxPlayers = maxPlayers,
            IpAddress = localIP,
            Port = port
        };

        // 启动主机
        NetworkManager.Instance.StartHost(port);

        Logger.Info($"Created: {CurrentRoom}", LogTag.Room);
    }

    public void TryJoinRoom(string ip, ushort port)
    {
        if (IsInRoom) LeaveRoom();

        NetworkManager.Instance.StartClient(ip, port);
    }

    public void LeaveRoom()
    {
        if (!IsInRoom) return;

        NetworkManager.Instance.ShutDown();
        CurrentRoom = null;

        Logger.Info("Left room", LogTag.Room);
    }

    public void EnterBattleScene()
    {
        if (!IsHost || !IsInRoom) return;
        Logger.Info("Starting battle...", LogTag.Room);

        NetworkManager.Instance.Broadcast(new GameStartMSG());
        HandleEnterBattleScene();
    }

    public void HandlePlayerJoinRequest(NetworkConnection conn, string playerName)
    {
        if(!IsInRoom) return;

        CurrentRoom = new RoomInfo
        {
            RoomId = CurrentRoom.Value.RoomId,
            HostName = CurrentRoom.Value.HostName,
            IpAddress = CurrentRoom.Value.IpAddress,
            Port = CurrentRoom.Value.Port,
            MaxPlayers = CurrentRoom.Value.MaxPlayers,
            PlayerCount = (byte)(CurrentRoom.Value.PlayerCount + 1)
        };


        NetworkManager.Instance.SendToClient(conn, new JoinResponseMSG()
        {
            assignedPlayerIndex = (byte)(CurrentRoom.Value.PlayerCount - 1)
        });

        NetworkManager.Instance.Broadcast(new RoomStateMSG
        {
            roomInfo = CurrentRoom.Value
        });

        Logger.Debug(CurrentRoom.Value.ToString());

        OnRoomInfoUpdated?.Invoke(CurrentRoom.Value);
    }

    public void HandlePlayerJoinResponse(byte playerIndex)
    {
        LocalPlayerIndex = playerIndex;
        UIManager.Instance.ShowPanelAsync<RoomPanel>().Forget();
    }

    public void HandleRoomStateUpdate(RoomInfo roomInfo)
    {
        CurrentRoom = roomInfo;
        OnRoomInfoUpdated?.Invoke(CurrentRoom.Value);
    }

    public void HandleEnterBattleScene()
    {
        UIManager.Instance.CloseAll();
        SceneLoader.LoadScene("BattleScene");
    }
}