using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetworkManager : SingletonMono<NetworkManager>
{
    NetworkDriver m_Driver;
    NativeList<NetworkConnection> m_Connections;
    NetworkConnection m_ClientConnection;

    bool m_IsServer = false;
    Dictionary<MessageId, Delegate> m_MessageHandlers = new();

    const int MAX_CONNECTIONS = 4;

    public void StartServer(ushort port = 7777)
    {
        if (m_Driver.IsCreated) m_Driver.Dispose();
        m_Driver = NetworkDriver.Create();
        m_Connections = new NativeList<NetworkConnection>(MAX_CONNECTIONS, Allocator.Persistent);

        var endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
        if (m_Driver.Bind(endpoint) != 0)
        {
            Debug.LogError($"Failed to bind server to port {port}");
            return;
        }
        m_Driver.Listen();
        m_IsServer = true;
        Debug.Log("Server started.");
    }

    public void StartClient(string ip, ushort port = 7777)
    {
        if (m_Driver.IsCreated) m_Driver.Dispose();
        m_Driver = NetworkDriver.Create();

        var endpoint = NetworkEndpoint.Parse(ip, port);
        m_ClientConnection = m_Driver.Connect(endpoint);
        m_IsServer = false;
        Debug.Log($"Client connecting to {ip}:{port}");
    }

    public void RegisterHandler<T>(MessageId id, Action<NetworkConnection, T> handler) where T : INetworkMessage, new()
    {
        m_MessageHandlers[id] = handler;
    }

    public void SendToServer<T>(MessageId id, T message) where T : INetworkMessage
    {
        if (!m_ClientConnection.IsCreated) return;
        SendInternal(m_ClientConnection, id, message);
    }

    public void SendToClient<T>(NetworkConnection conn, MessageId id, T message) where T : INetworkMessage
    {
        if (!conn.IsCreated || !m_Driver.IsCreated) return;
        SendInternal(conn, id, message);
    }

    public void Broadcast<T>(MessageId id, T message) where T : INetworkMessage
    {
        if (!m_IsServer) return;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (m_Connections[i].IsCreated)
                SendInternal(m_Connections[i], id, message);
        }
    }

    void SendInternal<T>(NetworkConnection conn, MessageId id, T message) where T : INetworkMessage
    {
        if (!m_Driver.IsCreated || !conn.IsCreated) return;

        m_Driver.BeginSend(NetworkPipeline.Null, conn, out var writer);
        writer.WriteByte((byte)id);
        message.Serialize(ref writer);
        m_Driver.EndSend(writer);
    }

    void Update()
    {
        if (!m_Driver.IsCreated) return;
        m_Driver.ScheduleUpdate().Complete();

        if (m_IsServer)
        {
            // Accept new connections
            NetworkConnection c;
            while ((c = m_Driver.Accept()) != default)
            {
                if (m_Connections.Length < MAX_CONNECTIONS)
                {
                    m_Connections.Add(c);
                    Debug.Log("New client connected.");
                }
                else
                {
                    c.Disconnect(m_Driver); // Reject if full
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
            // Client: only one connection
            ProcessIncoming(m_ClientConnection);
        }
    }

    void ProcessIncoming(NetworkConnection conn)
    {
        if (!conn.IsCreated) return;

        DataStreamReader stream;
        NetworkEvent.Type eventType;
        while ((eventType = m_Driver.PopEventForConnection(conn, out stream)) != NetworkEvent.Type.Empty)
        {
            switch (eventType)
            {
                case NetworkEvent.Type.Connect:
                    Debug.Log("Connected to server.");
                    break;

                case NetworkEvent.Type.Data:
                    HandleMessage(conn, ref stream);
                    break;

                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Disconnected.");
                    if (m_IsServer)
                    {
                        // In server, mark as dead (will be cleaned next frame)
                    }
                    else
                    {
                        m_ClientConnection = default;
                    }
                    break;
            }
        }
    }

    void HandleMessage(NetworkConnection conn, ref DataStreamReader stream)
    {
        byte msgIdByte = stream.ReadByte();
        if (!Enum.IsDefined(typeof(MessageId), msgIdByte))
        {
            Debug.LogWarning($"Unknown message ID: {msgIdByte}");
            return;
        }

        MessageId msgId = (MessageId)msgIdByte;

        if (m_MessageHandlers.TryGetValue(msgId, out var handler))
        {
            // Create message instance and deserialize
            var msgType = handler.Method.GetParameters()[1].ParameterType;
            var message = (INetworkMessage)Activator.CreateInstance(msgType);
            message.Deserialize(in stream);

            // Invoke handler via dynamic dispatch
            handler.DynamicInvoke(conn, message);
        }
        else
        {
            Debug.LogWarning($"No handler registered for message: {msgId}");
        }
    }

    protected override void OnSingletonDestroy()
    {
        base.OnSingletonDestroy();
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
            if (m_Connections.IsCreated)
                m_Connections.Dispose();
        }
    }
}