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

    const int MAX_CONNECTIONS = 4;

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

    public void StartClient(string ip, ushort port = 7777)
    {
        ShutDown();
        m_Driver = NetworkDriver.Create();

        var endpoint = NetworkEndpoint.Parse(ip, port);
        m_ClientConnection = m_Driver.Connect(endpoint);

        m_netRole = NetworkRole.Client;
        ClientState = ConnectionState.Connecting;
        m_ConnectionStartTime = Time.time; // 记录连接开始时间

        Logger.Info($"Client connecting to {ip}:{port}", LogTag.Net);
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

    void SendInternal<T>(NetworkConnection conn, T message) where T : INetworkMessage
    {
        if (!m_Driver.IsCreated || !conn.IsCreated) return;

        m_Driver.BeginSend(NetworkPipeline.Null, conn, out var writer);

        writer.WriteByte((byte)message.Id);

        message.Serialize(ref writer);

        m_Driver.EndSend(writer);
    }

    void Update()
    {
        if (!m_Driver.IsCreated) return;
        m_Driver.ScheduleUpdate().Complete();

        // 检查连接超时（仅客户端模式且正在连接中）
        if (m_netRole == NetworkRole.Client &&
            ClientState == ConnectionState.Connecting &&
            Time.time - m_ConnectionStartTime > CONNECTION_TIMEOUT)
        {
            OnConnectionFailed?.Invoke("Connection timeout");
            Logger.Error("Connection timeout", LogTag.Net);
            ShutDown();
            return;
        }

        if (m_netRole == NetworkRole.Host)
        {
            // Accept new connections
            NetworkConnection c;
            while ((c = m_Driver.Accept()) != default)
            {
                if (m_Connections.Length < MAX_CONNECTIONS)
                {
                    m_Connections.Add(c);
                    Logger.Info("New client connected.", LogTag.Net);
                }
                else
                {
                    c.Disconnect(m_Driver); // Reject if full
                    Logger.Warn("Rejected client connection: host full.", LogTag.Net);
                }
            }

            // Clean dead connections
            for (int i = 0; i < m_Connections.Length; i++)
            {
                if (!m_Connections[i].IsCreated)
                {
                    m_Connections.RemoveAtSwapBack(i);
                    i--;
                }
            }

            // Process messages from all clients
            for (int i = 0; i < m_Connections.Length; i++)
            {
                ProcessIncoming(m_Connections[i]);
            }
        }
        else
        {
            // 客户端：只处理一个连接
            ProcessIncoming(m_ClientConnection);
        }
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

            SendToHost(new JoinRequestMSG()
            {
                playerName = "Player" + UnityEngine.Random.Range(1000, 9999)
            });

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
            // 客户端断开连接
            ClientState = ConnectionState.Disconnected;
            Logger.Warn($"Disconnected from host.", LogTag.Net);
            OnSelfClientDisconnected?.Invoke();
            m_ClientConnection = default;
        }
        else if (m_netRole == NetworkRole.Host)
        {
            // 主机端的客户端断开
            Logger.Info($"Client disconnected. Connection: {conn}", LogTag.Net);
            // 可以在主机端处理客户端离开的逻辑
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
                    //OnPlayerInput(conn, in msg);
                    break;
                }

            case MessageId.JoinRequest:
                {
                    var msg = new JoinRequestMSG();
                    msg.Deserialize(ref stream);
                                        
                    RoomManager.Instance.HandlePlayerJoinRequest(conn, msg.playerName);
                    Logger.Info($"Received JoinRequest from player: {msg.playerName}", LogTag.Net);
                    break;
                }

            case MessageId.JoinResponse:
                {
                    var msg = new JoinResponseMSG();
                    msg.Deserialize(ref stream);

                    RoomManager.Instance.HandlePlayerJoinResponse(msg.assignedPlayerIndex);
                    Logger.Info($"Received JoinResponse: assignedPlayerIndex = {msg.assignedPlayerIndex}", LogTag.Net);
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

            case MessageId.PlayerBattleDataConfirmed:
                {
                    var msg = new PlayerBattleDataConfirmedMSG();
                    msg.Deserialize(ref stream);
                    
                    BattleManager.Instance.AddPlayer(msg.playerBattleData);
                    Logger.Info($"Received PlayerBattleDataConfirmed from PlayerIndex: {msg.playerBattleData.playerIndex}", LogTag.Net);
                    break;
                }

            case MessageId.BattleReady:
                {
                    var msg = new BattleReadyMSG();
                    msg.Deserialize(ref stream);

                    BattleManager.Instance.StartBattle(msg.startFrame, msg.randomSeed, msg.allPlayerBattleDatas);
                    Logger.Info("Received BattleReady message. Starting battle...", LogTag.Net);
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
        m_netRole = NetworkRole.None;
        Logger.Info("Network shut down.", LogTag.Net);
    }

    public static string GetLocalIPAddress()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return ip.ToString();
        }
        return "127.0.0.1";
#else
        return "127.0.0.1"; // 移动端需另处理
#endif
    }

    protected override void OnSingletonDestroy()
    {
        base.OnSingletonDestroy();
        ShutDown();
    }
}