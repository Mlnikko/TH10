using System;
using Unity.Networking.Transport;

public enum RoomJoinState
{
    None,
    Connecting,
    InRoom,
}

[Serializable]
public struct RoomInfo
{
    public string IpAddress;
    public ushort Port;

    public byte PlayerCount;
    public byte MaxPlayers;

    public override readonly string ToString()
    {
        return $"({PlayerCount}/{MaxPlayers}) @ {IpAddress}:{Port}";
    }
}

public class RoomManager : SingletonMono<RoomManager>
{
    public event Action<RoomInfo> OnRoomInfoUpdated;
    public event Action OnJoinStarted;
    public event Action OnJoinSucceeded;
    public event Action<string> OnJoinFailed;
    public event Action OnRoomLeft;
    public event Action<string> OnDisconnectedFromHost;

    public RoomInfo? CurrentRoom { get; private set; }
    public RoomJoinState JoinState { get; private set; } = RoomJoinState.None;
    public bool IsInRoom => JoinState == RoomJoinState.InRoom && CurrentRoom.HasValue;
    public bool IsHost => NetworkManager.Instance != null
        && NetworkManager.Instance.NetworkRole == NetworkRole.Host;
    public int PlayerCount => CurrentRoom?.PlayerCount ?? 0;

    public static byte LocalPlayerIndex;

    string _pendingJoinIp;
    ushort _pendingJoinPort;
    bool _networkEventsBound;

    protected override void OnSingletonInit()
    {
        BindNetworkEvents();
    }

    protected override void OnSingletonDestroy()
    {
        UnbindNetworkEvents();
        base.OnSingletonDestroy();
    }

    void BindNetworkEvents()
    {
        if (_networkEventsBound)
            return;

        NetworkManager.OnConnectionFailed += HandleConnectionFailed;
        NetworkManager.OnSelfClientDisconnected += HandleSelfClientDisconnected;
        NetworkManager.OnHostClientDisconnected += HandleHostClientDisconnected;
        _networkEventsBound = true;
    }

    void UnbindNetworkEvents()
    {
        if (!_networkEventsBound)
            return;

        NetworkManager.OnConnectionFailed -= HandleConnectionFailed;
        NetworkManager.OnSelfClientDisconnected -= HandleSelfClientDisconnected;
        NetworkManager.OnHostClientDisconnected -= HandleHostClientDisconnected;
        _networkEventsBound = false;
    }

    public void CreateRoom(ushort port = 7777, byte maxPlayers = 4)
    {
        ResetSession();

        LocalPlayerIndex = 0;

        string localIP = NetworkTool.GetLocalIPAddress();

        CurrentRoom = new RoomInfo
        {
            PlayerCount = 1,
            MaxPlayers = maxPlayers,
            IpAddress = localIP,
            Port = port
        };

        NetworkManager.Instance.StartHost(port);
        JoinState = RoomJoinState.InRoom;

        Logger.Info($"Created: {CurrentRoom}", LogTag.Room);
        OnRoomInfoUpdated?.Invoke(CurrentRoom.Value);
    }

    public bool TryJoinRoom(string ip, ushort port)
    {
        BindNetworkEvents();

        ip = ip?.Trim();
        if (!NetworkTool.IsValidHostAddress(ip))
        {
            NotifyJoinFailed("IP 地址格式无效");
            return false;
        }

        ResetSession();

        _pendingJoinIp = ip;
        _pendingJoinPort = port;
        JoinState = RoomJoinState.Connecting;
        OnJoinStarted?.Invoke();

        if (!NetworkManager.Instance.StartClient(ip, port))
        {
            FailJoin("无法连接到目标地址，请检查 IP 和端口");
            return false;
        }

        Logger.Info($"Joining room at {ip}:{port}", LogTag.Room);
        return true;
    }

    public void CancelJoinAttempt()
    {
        if (JoinState != RoomJoinState.Connecting)
            return;

        ResetSession();
        Logger.Info("Join attempt cancelled.", LogTag.Room);
    }

    public void LeaveRoom()
    {
        if (JoinState == RoomJoinState.None)
            return;

        var net = NetworkManager.Instance;
        if (IsHost && net != null && net.NetworkRole == NetworkRole.Host)
        {
            net.Broadcast(new RoomHostLeaveMSG());
            net.FlushOutgoing();
        }

        ResetSession();
        Logger.Info("Left room", LogTag.Room);
        OnRoomLeft?.Invoke();
    }

    /// <summary>房主离开房间时，客户端收到广播后自动退出。</summary>
    public void HandleHostLeftRoom()
    {
        if (JoinState == RoomJoinState.None || IsHost)
            return;

        ResetSession();
        Logger.Info("Host left room; local session cleared.", LogTag.Room);
        OnDisconnectedFromHost?.Invoke("房主已离开房间");
    }

    public void EnterBattleScene()
    {
        if (!IsHost || !IsInRoom) return;
        Logger.Info("Starting battle...", LogTag.Room);
        NetworkManager.Instance.Broadcast(new GameStartMSG());
        HandleEnterBattleScene();
    }

    public void HandlePlayerJoinRequest(NetworkConnection conn)
    {
        if (!IsHost || !IsInRoom)
            return;

        var room = CurrentRoom.Value;
        if (room.PlayerCount >= room.MaxPlayers)
        {
            NetworkManager.Instance.SendToClient(conn, new JoinResponseMSG { accepted = false });
            NetworkManager.Instance.DisconnectClient(conn);
            Logger.Warn("Rejected join request: room full.", LogTag.Room);
            return;
        }

        CurrentRoom = new RoomInfo
        {
            IpAddress = room.IpAddress,
            Port = room.Port,
            MaxPlayers = room.MaxPlayers,
            PlayerCount = (byte)(room.PlayerCount + 1)
        };

        byte assignedIndex = (byte)(CurrentRoom.Value.PlayerCount - 1);
        NetworkManager.Instance.SendToClient(conn, new JoinResponseMSG
        {
            accepted = true,
            assignedPlayerIndex = assignedIndex,
            roomInfo = CurrentRoom.Value
        });

        NetworkManager.Instance.Broadcast(new RoomStateMSG
        {
            roomInfo = CurrentRoom.Value
        });

        Logger.Debug(CurrentRoom.Value.ToString(), LogTag.Room);
        OnRoomInfoUpdated?.Invoke(CurrentRoom.Value);
    }

    public void HandlePlayerJoinResponse(in JoinResponseMSG response)
    {
        if (JoinState != RoomJoinState.Connecting)
            return;

        if (!response.accepted)
        {
            FailJoin("房间已满或无法加入");
            return;
        }

        LocalPlayerIndex = response.assignedPlayerIndex;
        CurrentRoom = response.roomInfo;
        JoinState = RoomJoinState.InRoom;
        _pendingJoinIp = null;

        Logger.Info($"Joined room as player {LocalPlayerIndex}: {CurrentRoom}", LogTag.Room);
        OnJoinSucceeded?.Invoke();
        OnRoomInfoUpdated?.Invoke(CurrentRoom.Value);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ClosePanel<JoinRoomInputPanel>();
            UIManager.Instance.ShowPanelAsync<RoomPanel>().Forget();
        }
    }

    public void HandleRoomStateUpdate(RoomInfo roomInfo)
    {
        if (JoinState == RoomJoinState.None)
            return;

        CurrentRoom = roomInfo;
        OnRoomInfoUpdated?.Invoke(CurrentRoom.Value);
    }

    public void HandleEnterBattleScene()
    {
        BattleManager.Instance.LoadBattleSceneAndShowPrepareAsync().Forget();
    }

    void HandleConnectionFailed(string reason)
    {
        if (JoinState != RoomJoinState.Connecting)
            return;

        FailJoin(TranslateConnectionError(reason));
    }

    void HandleSelfClientDisconnected()
    {
        if (JoinState == RoomJoinState.Connecting)
        {
            FailJoin("连接已断开");
            return;
        }

        if (IsInRoom && !IsHost)
        {
            string message = "已与主机断开连接";
            ResetSession();
            OnDisconnectedFromHost?.Invoke(message);
        }
    }

    void HandleHostClientDisconnected()
    {
        if (!IsHost || !IsInRoom || !CurrentRoom.HasValue)
            return;

        var room = CurrentRoom.Value;
        if (room.PlayerCount <= 1)
            return;

        CurrentRoom = new RoomInfo
        {
            IpAddress = room.IpAddress,
            Port = room.Port,
            MaxPlayers = room.MaxPlayers,
            PlayerCount = (byte)(room.PlayerCount - 1)
        };

        NetworkManager.Instance.Broadcast(new RoomStateMSG
        {
            roomInfo = CurrentRoom.Value
        });

        Logger.Info($"Client left room. PlayerCount={CurrentRoom.Value.PlayerCount}", LogTag.Room);
        OnRoomInfoUpdated?.Invoke(CurrentRoom.Value);
    }

    void ResetSession()
    {
        if (NetworkManager.Instance != null)
            NetworkManager.Instance.ShutDown();

        CurrentRoom = null;
        JoinState = RoomJoinState.None;
        _pendingJoinIp = null;
        _pendingJoinPort = 0;
    }

    void FailJoin(string message)
    {
        ResetSession();
        NotifyJoinFailed(message);
    }

    void NotifyJoinFailed(string message)
    {
        Logger.Warn(message, LogTag.Room);
        OnJoinFailed?.Invoke(message);
    }

    static string TranslateConnectionError(string reason) =>
        reason switch
        {
            "Connection timeout" => "连接超时，请检查 IP 和端口",
            "Connection failed" => "连接失败，请检查 IP 和端口",
            _ => string.IsNullOrEmpty(reason) ? "连接失败" : reason
        };
}
