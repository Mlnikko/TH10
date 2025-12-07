using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;

public enum NetworkRole
{
    None = 0,
    Host = 1,
    Client = 2
}

public enum NetworkStatus
{
    Disconnected = 0,
    Connected = 2
}

public class NetworkManager : SingletonMono<NetworkManager>
{
    public NetworkRole netRole = NetworkRole.None;
    public NetworkStatus netStatus = NetworkStatus.Disconnected;

    public string ipAddress = "127.0.0.1";
    public ushort port = 7777;

    // ====== 内部状态 ======
    NetworkDriver _driver;
    NetworkPipeline _pipeline;

    NetworkConnection _connection;
    readonly List<NetworkConnection> _clients = new();

    public byte LocalPlayerIndex => _localPlayerIndex;
    byte _localPlayerIndex;

    // ====== 消息系统 ======
    private readonly Dictionary<byte, Action<DataStreamReader>> _handlers = new();
    private static readonly Dictionary<Type, byte> MessageTypeMap = new();

    protected override void OnSingletonInit()
    {
        base.OnSingletonInit();
 
        RegisterMessageType<InputMSG>(1);
    }

    // 注册消息类型（必须在运行前调用，建议在 Awake 或静态初始化）
    public static void RegisterMessageType<T>(byte id) where T : INetworkMessage, new()
    {
        MessageTypeMap[typeof(T)] = id;
    }

    // 注册处理器（运行时）
    public void RegisterHandler<T>(Action<T> handler) where T : INetworkMessage, new()
    {
        if (!MessageTypeMap.TryGetValue(typeof(T), out byte id))
            throw new ArgumentException($"Message type {typeof(T)} not registered!");

        _handlers[id] = (reader) =>
        {
            var msg = new T();
            msg.Deserialize(reader);
            handler?.Invoke(msg);
        };
    }

    // ====== 初始化（保持你的逻辑，加 Shutdown）=====
    public void StartHost(byte localPlayerIndex = 0)
    {
        Shutdown();

        _localPlayerIndex = localPlayerIndex;
        netRole = NetworkRole.Host;
        netStatus = NetworkStatus.Connected;

        _driver = NetworkDriver.Create();
        _pipeline = NetworkPipeline.Null;
        _driver.Bind(NetworkEndpoint.AnyIpv4.WithPort(port));
        _driver.Listen();

        Logger.Debug($"Host started on port {port}", LogTag.Network);
    }

    public void StartClient(string ip, ushort remotePort, byte localPlayerIndex = 1)
    {
        Shutdown();

        _localPlayerIndex = localPlayerIndex;
        netRole = NetworkRole.Client;

        ipAddress = ip;
        port = remotePort;

        _driver = NetworkDriver.Create();
        _pipeline = NetworkPipeline.Null;

        if (!NetworkEndpoint.TryParse(ip, remotePort, out var endpoint))
        {
            Logger.Error($"Invalid endpoint: {ip}:{remotePort}", LogTag.Network);
            return;
        }

        _connection = _driver.Connect(endpoint);
        Logger.Debug($"Connecting to {endpoint}", LogTag.Network);
    }

    // ====== 通用发送方法 ======
    public void SendToServer<T>(in T message) where T : INetworkMessage
    {
        if (netRole != NetworkRole.Client || netStatus != NetworkStatus.Connected) return;
        if (!_connection.IsCreated) return;

        SendInternal(_connection, message);
    }

    public void SendToAllClients<T>(in T message) where T : INetworkMessage
    {
        if (netRole != NetworkRole.Host) return;
        for (int i = _clients.Count - 1; i >= 0; i--)
        {
            var conn = _clients[i];
            if (conn.IsCreated && _driver.GetConnectionState(conn) == NetworkConnection.State.Connected)
            {
                SendInternal(conn, message);
            }
            else
            {
                // 客户端已断开，清理
                OnRemoteDisconnected(conn);
                _clients.RemoveAt(i);
            }
        }
    }

    public void SendToClient<T>(NetworkConnection conn, in T message) where T : INetworkMessage
    {
        if (netRole != NetworkRole.Host) return;
        if (conn.IsCreated && _driver.GetConnectionState(conn) == NetworkConnection.State.Connected)
        {
            SendInternal(conn, message);
        }
    }

    void SendInternal<T>(NetworkConnection conn, in T message) where T : INetworkMessage
    {
        if (!_driver.IsCreated || !MessageTypeMap.TryGetValue(typeof(T), out byte id)) return;

        if (_driver.BeginSend(_pipeline, conn, out DataStreamWriter writer) >= 0)
        {
            writer.WriteByte(id);
            message.Serialize(ref writer);
            _driver.EndSend(writer);
        }
    }

    // ====== 网络轮询 =====
    public void PollNetwork()
    {
        if (netRole == NetworkRole.None || !_driver.IsCreated) return;

        _driver.ScheduleUpdate().Complete();

        // Host: 接受新连接
        if (netRole == NetworkRole.Host)
        {
            NetworkConnection c;
            while ((c = _driver.Accept()) != default)
            {
                if (_clients.Count < 3)
                {
                    _clients.Add(c);
                    Logger.Debug($"Client connected. Total: {_clients.Count + 1}", LogTag.Network);
                }
                else
                {
                    _driver.Disconnect(c);
                }
            }
        }

        // 处理所有连接
        ProcessConnections();

        // Client 连接确认
        if (netRole == NetworkRole.Client && netStatus != NetworkStatus.Disconnected)
        {
            if (_connection.IsCreated &&
                _driver.GetConnectionState(_connection) == NetworkConnection.State.Connected)
            {
                netStatus = NetworkStatus.Connected;
                Logger.Debug($"Connected to host!", LogTag.Network);
            }
        }
    }

    void ProcessConnections()
    {
        var connections = netRole == NetworkRole.Host ? _clients : new List<NetworkConnection> { _connection };

        for (int i = connections.Count - 1; i >= 0; i--)
        {
            var conn = connections[i];
            if (!conn.IsCreated) continue;

            NetworkEvent.Type eventType;
            while ((eventType = _driver.PopEventForConnection(conn, out DataStreamReader stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    case NetworkEvent.Type.Data:
                        HandleMessage(stream);
                        break;

                    case NetworkEvent.Type.Disconnect:
                        OnRemoteDisconnected(conn);
                        if (netRole == NetworkRole.Host)
                        {
                            _clients.Remove(conn); // 安全移除
                        }
                        break;

                    case NetworkEvent.Type.Connect:
                        // UDP 下通常不触发，可忽略
                        break;
                }
            }
        }
    }

    void HandleMessage(DataStreamReader stream)
    {
        if (stream.Length < 1) return;

        byte msgId = stream.ReadByte();
        if (_handlers.TryGetValue(msgId, out var handler))
        {
            handler(stream);
        }
        else
        {
            Logger.Warn($"Unhandled message ID: {msgId}", LogTag.Network);
        }
    }

    // ====== 断开处理（统一入口）=====
    void OnRemoteDisconnected(NetworkConnection conn)
    {
        Logger.Debug("Remote disconnected", LogTag.Network);

        // 触发全局事件（可选）
        OnClientDisconnected?.Invoke(conn);

        if (netRole == NetworkRole.Client)
        {
            netStatus = NetworkStatus.Disconnected;
            _connection = default;
        }
    }

    // ====== 事件（供外部监听）=====
    public Action<NetworkConnection> OnClientDisconnected;

    // ====== 清理 ======
    public void Shutdown()
    {
        if (_driver.IsCreated)
        {
            if (netRole == NetworkRole.Host)
            {
                foreach (var c in _clients)
                {
                    if (c.IsCreated) _driver.Disconnect(c);
                }
            }
            else if (netRole == NetworkRole.Client && _connection.IsCreated)
            {
                _driver.Disconnect(_connection);
            }
            _driver.Dispose();
        }

        _clients.Clear();
        _connection = default;
        netRole = NetworkRole.None;
        netStatus = NetworkStatus.Disconnected;
        _handlers.Clear();
    }

    protected override void OnSingletonDestroy()
    {
        base.OnSingletonDestroy();
        Shutdown();
    }

    void OnApplicationQuit()
    {
        Shutdown();
    }
}