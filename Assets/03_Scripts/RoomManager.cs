using System;
using Unity.Networking.Transport;
using UnityEngine;

[Serializable]
public struct RoomInfo
{
    public int RoomId;
    public string HostName;
    public byte PlayerCount;
    public byte MaxPlayers;
    public string IpAddress;
    public int Port;

    public bool IsFull => PlayerCount >= MaxPlayers;

    public override string ToString()
    {
        return $"[{RoomId}] {HostName} ({PlayerCount}/{MaxPlayers}) @ {IpAddress}:{Port}";
    }
}

public class RoomManager : SingletonMono<RoomManager>
{
    // ====== 事件（UI 用）======
    public event Action OnRoomCreated;
    public event Action OnJoinedRoom;
    public event Action OnLeftRoom;
    public event Action<int> OnPlayerCountChanged; // 玩家数变化（来自网络）

    // ====== 状态 ======
    public RoomInfo? CurrentRoom { get; private set; }
    public bool IsInRoom => CurrentRoom.HasValue;
    //public bool IsHost => NetworkManager.Instance.netRole == NetworkRole.Host;

    public byte playerIndex;

    // ====== 初始化 ======
    protected override void OnSingletonInit()
    {
        base.OnSingletonInit();

        // 监听网络断开（自动退出房间）
        //NetworkManager.Instance.OnClientDisconnected += OnNetworkDisconnected;
    }

    protected override void OnSingletonDestroy()
    {
        //NetworkManager.Instance.OnClientDisconnected -= OnNetworkDisconnected;
        base.OnSingletonDestroy();
    }

    // ====== 房间操作 ======

    public void CreateRoom(string hostName, byte maxPlayers = 4)
    {
        if (IsInRoom) LeaveRoom();

        // 获取本机局域网 IP（需实现 GetLocalIP()）
        string localIP = GetLocalIPAddress();
        int port = 7777;

        var room = new RoomInfo
        {
            RoomId = UnityEngine.Random.Range(10000, 99999),
            HostName = hostName,
            PlayerCount = 1,
            MaxPlayers = maxPlayers,
            IpAddress = localIP,
            Port = port
        };

        CurrentRoom = room;

        // 启动主机
        //NetworkManager.Instance.StartHost();

        OnRoomCreated?.Invoke();
        Logger.Info($"[ROOM] Created: {room}");
    }

    public void JoinRoom(string ip, ushort port)
    {
        if (IsInRoom) LeaveRoom();

        //NetworkManager.Instance.StartClient(ip, port);

        OnJoinedRoom?.Invoke();

        Logger.Debug("尝试加入房间");
    }

    public void LeaveRoom()
    {
        if (!IsInRoom) return;

        //NetworkManager.Instance.Shutdown(); // 断开所有连接
        CurrentRoom = null;

        OnLeftRoom?.Invoke();
        Logger.Info("[ROOM] Left room");
    }

    // ====== 网络回调 ======

    void OnNetworkDisconnected(NetworkConnection conn)
    {
        // 任何网络断开都视为离开房间
        if (IsInRoom)
        {
            CurrentRoom = null;
            OnLeftRoom?.Invoke();
            Logger.Warn("[ROOM] Disconnected from host");
        }
    }

    // ====== 工具方法 ======

    string GetLocalIPAddress()
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


    // ====== 外部调用：更新玩家数（由网络消息触发）=====
    internal void UpdatePlayerCount(byte newCount)
    {
        if (!CurrentRoom.HasValue) return;
        var updated = CurrentRoom.Value;
        updated.PlayerCount = newCount;
        CurrentRoom = updated;
        OnPlayerCountChanged?.Invoke(newCount);
    }
}