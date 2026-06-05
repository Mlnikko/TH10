using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public enum NetworkRole
{
    None = 0,
    Host = 1,
    Client = 2
}

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Connected
}

/// <summary>先于 <see cref="BattleManager"/> 收包，避免本渲染帧内输入晚于锁步就绪检查。</summary>
[DefaultExecutionOrder(-100)]
public class NetworkManager : SingletonMono<NetworkManager>
{
    public NetworkRole NetworkRole => m_netRole;
    [SerializeField] NetworkRole m_netRole = NetworkRole.None;
    NetworkDriver m_Driver;

    // 客户端相关
    NetworkConnection m_ClientConnection;
    ConnectionState ClientState = ConnectionState.Disconnected;
    float m_ConnectionStartTime = 0f;
    const float CONNECTION_TIMEOUT = 5f; // 5秒超时

    // 主机相关
    NativeList<NetworkConnection> m_Connections;

    public static event Action OnSelfClientConnected;      // 客户端连接成功
    public static event Action OnSelfClientDisconnected;   // 客户端断开连接
    public static event Action<string> OnConnectionFailed; // 连接失败
    public static event Action OnHostClientDisconnected;   // 主机端有客户端离开

    const int MAX_CONNECTIONS = 4;

    public ConnectionState ClientConnectionState => ClientState;
    public bool IsClientConnecting =>
        m_netRole == NetworkRole.Client && ClientState == ConnectionState.Connecting;

    public void StartHost(ushort port = 7777)
    {
        ShutDown();
        m_Driver = NetworkDriver.Create();
        m_Connections = new NativeList<NetworkConnection>(MAX_CONNECTIONS, Allocator.Persistent);

        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
        if (m_Driver.Bind(endpoint) != 0)
        {
            Logger.Error($"Failed to bind host to port {port}", LogTag.Net);
            return;
        }
        m_Driver.Listen();
        m_netRole = NetworkRole.Host;
        Logger.Info("Host started.", LogTag.Net);
    }

    public bool StartClient(string ip, ushort port = 7777)
    {
        ShutDown();

        if (!NetworkTool.TryCreateClientEndpoint(ip, port, out var endpoint))
        {
            Logger.Error($"Invalid client endpoint: {ip}:{port}", LogTag.Net);
            return false;
        }

        m_Driver = NetworkDriver.Create();
        m_ClientConnection = m_Driver.Connect(endpoint);

        m_netRole = NetworkRole.Client;
        ClientState = ConnectionState.Connecting;
        m_ConnectionStartTime = Time.time;

        Logger.Info($"Client connecting to {ip}:{port}", LogTag.Net);
        return true;
    }

    public void DisconnectClient(NetworkConnection conn)
    {
        if (!m_Driver.IsCreated || !conn.IsCreated)
            return;

        conn.Disconnect(m_Driver);
    }

    public void SendToHost<T>(T message) where T : INetworkMessage
    {
        if (!m_ClientConnection.IsCreated) return;
        SendInternal(m_ClientConnection, message);
    }

    public void SendToClient<T>(NetworkConnection conn, T message) where T : INetworkMessage
    {
        if (!conn.IsCreated || !m_Driver.IsCreated) return;
        SendInternal(conn, message);
    }

    public void Broadcast<T>(T message) where T : INetworkMessage
    {
        if (m_netRole != NetworkRole.Host) return;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (m_Connections[i].IsCreated)
                SendInternal(m_Connections[i], message);
        }
    }

    /// <summary>刷新网络驱动发送队列，确保广播在 ShutDown 前发出。</summary>
    public void FlushOutgoing()
    {
        if (!m_Driver.IsCreated)
            return;

        m_Driver.ScheduleUpdate().Complete();
    }

    void SendInternal<T>(NetworkConnection conn, T message) where T : INetworkMessage
    {
        if (!m_Driver.IsCreated || !conn.IsCreated) return;

        m_Driver.BeginSend(NetworkPipeline.Null, conn, out var writer);

        writer.WriteByte((byte)message.Id);

        message.Serialize(ref writer);

        m_Driver.EndSend(writer);
    }

    /// <summary>刷新网络驱动并处理本帧已到达的消息（可在锁步前再次调用）。</summary>
    public void PumpNetwork()
    {
        if (!m_Driver.IsCreated)
            return;

        m_Driver.ScheduleUpdate().Complete();
        ProcessPendingConnectionsAndMessages();
    }

    void ProcessPendingConnectionsAndMessages()
    {
        if (m_netRole == NetworkRole.Host)
        {
            NetworkConnection c;
            while ((c = m_Driver.Accept()) != default)
            {
                int maxClients = MAX_CONNECTIONS - 1;
                if (RoomManager.Instance != null
                    && RoomManager.Instance.IsInRoom
                    && RoomManager.Instance.CurrentRoom.HasValue)
                {
                    maxClients = Math.Max(0, RoomManager.Instance.CurrentRoom.Value.MaxPlayers - 1);
                }

                if (m_Connections.Length < maxClients)
                {
                    m_Connections.Add(c);
                    Logger.Info("New client connected.", LogTag.Net);
                }
                else
                {
                    c.Disconnect(m_Driver);
                    Logger.Warn("Rejected client connection: room full.", LogTag.Net);
                }
            }

            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                {
                    m_Connections.RemoveAtSwapBack(i);
                    i--;
                }
            }

            for (int i = 0; i < m_Connections.Length; i++)
                ProcessIncoming(m_Connections[i]);
        }
        else if (m_netRole == NetworkRole.Client)
        {
            ProcessIncoming(m_ClientConnection);
        }
    }

    void Update()
    {
        if (!m_Driver.IsCreated) return;

        PumpNetwork();
        if (m_netRole == NetworkRole.Client &&
            ClientState == ConnectionState.Connecting &&
            Time.time - m_ConnectionStartTime > CONNECTION_TIMEOUT)
        {
            OnConnectionFailed?.Invoke("Connection timeout");
            Logger.Error("Connection timeout", LogTag.Net);
            ShutDown();
            return;
        }

        PingTest();
    }

    NetworkStatusHud _networkStatusHud;

    void LateUpdate()
    {
        RefreshNetworkStatusHud();
    }

    void RefreshNetworkStatusHud()
    {
        if (m_netRole == NetworkRole.None)
        {
            if (_networkStatusHud != null)
                _networkStatusHud.SetVisible(false);
            return;
        }

        if (UIManager.Instance == null || UIManager.Instance.Canvas == null)
        {
            if (_networkStatusHud != null)
                _networkStatusHud.SetVisible(false);
            return;
        }

        _networkStatusHud ??= NetworkStatusHud.GetOrCreate(UIManager.Instance.Canvas.transform);
        if (_networkStatusHud == null)
            return;

        _networkStatusHud.SetVisible(true);

        string statusText;
        if (m_netRole == NetworkRole.Client)
            statusText = $"[CLIENT]\nRTT: {(CurrentRTT >= 0 ? CurrentRTT.ToString("F0") + " ms" : "—")}";
        else
            statusText = "[HOST]";

        _networkStatusHud.SetStatusText(statusText);
    }

    void ProcessIncoming(NetworkConnection conn)
    {
        if (!conn.IsCreated) return;

        NetworkEvent.Type eventType;
        while ((eventType = m_Driver.PopEventForConnection(conn, out DataStreamReader stream)) != NetworkEvent.Type.Empty)
        {
            switch (eventType)
            {
                case NetworkEvent.Type.Connect:
                    OnNetworkConnected(conn);
                    break;

                case NetworkEvent.Type.Data:
                    HandleMessage(conn, ref stream);
                    break;

                case NetworkEvent.Type.Disconnect:
                    OnNetworkDisconnected(conn);
                    break;
            }
        }
    }

    void OnNetworkConnected(NetworkConnection conn)
    {
        if (m_netRole == NetworkRole.Client)
        {
            // 客户端连接成功
            ClientState = ConnectionState.Connected;
            Logger.Info($"Connected to host successfully. Connection: {conn}", LogTag.Net);

            SendToHost(new JoinRequestMSG());

            OnSelfClientConnected?.Invoke();
        }
        else if (m_netRole == NetworkRole.Host)
        {
            // 主机端有客户端连接
            Logger.Info($"Client connected. Connection: {conn}", LogTag.Net);
            // 这里可以触发主机端的客户端连接事件
        }
    }

    void OnNetworkDisconnected(NetworkConnection conn)
    {
        if (m_netRole == NetworkRole.Client)
        {
            bool wasConnecting = ClientState == ConnectionState.Connecting;
            ClientState = ConnectionState.Disconnected;
            Logger.Warn("Disconnected from host.", LogTag.Net);
            m_ClientConnection = default;

            if (wasConnecting)
                OnConnectionFailed?.Invoke("Connection failed");
            else
                OnSelfClientDisconnected?.Invoke();
        }
        else if (m_netRole == NetworkRole.Host)
        {
            Logger.Info($"Client disconnected. Connection: {conn}", LogTag.Net);
            OnHostClientDisconnected?.Invoke();
        }
    }

    void HandleMessage(NetworkConnection conn, ref DataStreamReader stream)
    {
        var msgId = (MessageId)stream.ReadByte();

        switch (msgId)
        {
            case MessageId.PlayerInput:
                {
                    var msg = new InputMSG();
                    msg.Deserialize(ref stream);

                    if(NetworkRole == NetworkRole.Host)
                    {
                        InputManager.Instance.AddRemoteInput(msg.frameInput);
                        Broadcast(msg);
                    }
                    else if(NetworkRole == NetworkRole.Client)
                    {
                        InputManager.Instance.AddRemoteInput(msg.frameInput);
                    }
                    break;
                }

            case MessageId.JoinRequest:
                {
                    var msg = new JoinRequestMSG();
                    msg.Deserialize(ref stream);
                                        
                    RoomManager.Instance.HandlePlayerJoinRequest(conn);
                    Logger.Info("Received JoinRequest.", LogTag.Net);
                    break;
                }

            case MessageId.JoinResponse:
                {
                    var msg = new JoinResponseMSG();
                    msg.Deserialize(ref stream);

                    RoomManager.Instance.HandlePlayerJoinResponse(in msg);
                    Logger.Info(
                        msg.accepted
                            ? $"Received JoinResponse: assignedPlayerIndex = {msg.assignedPlayerIndex}"
                            : "Received JoinResponse: rejected",
                        LogTag.Net);
                    break;
                }

            case MessageId.RoomState:
                {
                    var msg = new RoomStateMSG();
                    msg.Deserialize(ref stream);

                    var roomInfo = msg.roomInfo;
                    Logger.Debug(roomInfo.ToString(), LogTag.Net);

                    RoomManager.Instance.HandleRoomStateUpdate(msg.roomInfo);
                    Logger.Info($"Received RoomState update: PlayerCount = {msg.roomInfo.PlayerCount}", LogTag.Net);
                    break;
                }

            case MessageId.StartGame:
                {           
                    // 无需反序列化内容
                    RoomManager.Instance.HandleEnterBattleScene();
                    Logger.Info("Received StartGame message.", LogTag.Net);
                    break;
                }

            case MessageId.BattleReady:
                {
                    var msg = new BattleReadyMSG();
                    msg.Deserialize(ref stream);

                    if (m_netRole == NetworkRole.Host)
                        BattleManager.Instance.HostReceiveClientPrepareReady(msg.playerBattleData);
                    else
                        BattleManager.Instance.ClientApplyPrepareReadyBroadcast(msg.playerBattleData);

                    Logger.Info($"Received BattleReady from PlayerIndex: {msg.playerBattleData.playerIndex}", LogTag.Net);
                    break;
                }

            case MessageId.BattlePrepareCancel:
                {
                    var msg = new BattlePrepareCancelMSG();
                    msg.Deserialize(ref stream);

                    if (m_netRole == NetworkRole.Host)
                        BattleManager.Instance.HostReceiveClientPrepareCancel(msg.playerIndex);
                    else
                        BattleManager.Instance.ClientApplyPrepareCancelBroadcast(msg.playerIndex);

                    Logger.Info($"Received BattlePrepareCancel from PlayerIndex: {msg.playerIndex}", LogTag.Net);
                    break;
                }

            case MessageId.BattleStart:
                {
                    var msg = new BattleStartMSG();
                    msg.Deserialize(ref stream);

                    BattleManager.Instance.StartMutiPlayerBattleForClient(msg.startFrame, msg.randomSeed, msg.playerDatas);
                    Logger.Info("Received BattleStart message. Starting battle...", LogTag.Net);
                    break;
                }

            case MessageId.BattlePauseApply:
                {
                    var msg = new BattlePauseApplyMSG();
                    msg.Deserialize(ref stream);
                    BattleManager.Instance.ClientApplyBattlePause();
                    break;
                }

            case MessageId.BattlePauseResume:
                {
                    var msg = new BattlePauseResumeMSG();
                    msg.Deserialize(ref stream);
                    BattleManager.Instance.ClientApplyBattleResume();
                    break;
                }

            case MessageId.BattlePauseReturnToRoom:
                {
                    var msg = new BattlePauseReturnToRoomMSG();
                    msg.Deserialize(ref stream);
                    BattleManager.Instance.ClientApplyBattleReturnToRoom();
                    break;
                }

            case MessageId.BattleGameOver:
                {
                    var msg = new BattleGameOverMSG();
                    msg.Deserialize(ref stream);
                    BattleManager.Instance.ClientApplyBattleGameOver();
                    break;
                }

            case MessageId.BattleStageClear:
                {
                    var msg = new BattleStageClearMSG();
                    msg.Deserialize(ref stream);
                    BattleManager.Instance.ClientApplyBattleStageClear();
                    break;
                }

            case MessageId.BattleRestart:
                {
                    var msg = new BattleRestartMSG();
                    msg.Deserialize(ref stream);
                    BattleManager.Instance.ClientApplyBattleRestart(msg.playerDatas);
                    break;
                }

            case MessageId.RoomHostLeave:
                {
                    var msg = new RoomHostLeaveMSG();
                    msg.Deserialize(ref stream);
                    RoomManager.Instance.HandleHostLeftRoom();
                    Logger.Info("Received RoomHostLeave message.", LogTag.Net);
                    break;
                }

            case MessageId.PingRequest:
                {
                    var msg = new PingRequestMSG();
                    msg.Deserialize(ref stream);

                    // 主机收到 Ping，立即回复
                    var response = new PingResponseMSG { timestamp = msg.timestamp };
                    if (m_netRole == NetworkRole.Host)
                    {
                        SendToClient(conn, response);
                    }
                    break;
                }

            case MessageId.PingResponse:
                {
                    var msg = new PingResponseMSG();
                    msg.Deserialize(ref stream);

                    // 客户端收到响应
                    if (m_netRole == NetworkRole.Client && msg.timestamp == m_PendingPingId)
                    {
                        float rtt = (Time.time - m_PingSentTime) * 1000f; // 转为毫秒
                        CurrentRTT = Mathf.Round(rtt);
                        // Logger.Debug($"Ping RTT: {CurrentRTT} ms", LogTag.Net);
                    }
                    break;
                }

            default:
                Logger.Warn($"Unknown message ID: {(byte)msgId}", LogTag.Net);
                break;
        }
    }

    public void ShutDown()
    {
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
            if (m_Connections.IsCreated)
                m_Connections.Dispose();
        }

        m_ClientConnection = default;
        ClientState = ConnectionState.Disconnected;
        m_netRole = NetworkRole.None;
        Logger.Info("Network shut down.", LogTag.Net);
    }

    protected override void OnSingletonDestroy()
    {
        base.OnSingletonDestroy();
        ShutDown();
    }

    #region Ping
    float m_LastPingTime = 0f;
    const float PING_INTERVAL = 1.0f; // 每秒 ping 一次

    // 存储最近一次 RTT（单位：毫秒）
    public static float CurrentRTT { get; private set; } = -1f;

    // 用于生成唯一时间戳（避免跨平台 DateTime 精度问题）
    static uint s_TimeStampCounter = 0;

    void PingTest()
    {
        // 自动发送 Ping（仅客户端）
        if (m_netRole == NetworkRole.Client &&
            ClientState == ConnectionState.Connected &&
            Time.time - m_LastPingTime > PING_INTERVAL)
        {
            m_LastPingTime = Time.time;
            SendPing();
        }
    }

    void SendPing()
    {
        if (m_netRole == NetworkRole.Client && ClientState == ConnectionState.Connected)
        {
            var msg = new PingRequestMSG
            {
                timestamp = ++s_TimeStampCounter // 简单递增 ID 作为“时间戳”
            };
            SendToHost(msg);
            m_PingSentTime = Time.time;
            m_PendingPingId = msg.timestamp;
        }
    }

    private uint m_PendingPingId = 0;
    private float m_PingSentTime = 0f;
    #endregion
}