using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RoomInfo
{
    public int RoomId;
    public string HostName;     // 主机名（用于显示）
    public int PlayerCount;
    public int MaxPlayers;
    public string IpAddress;    // 主机 IP（用于直连）

    public bool IsFull => PlayerCount >= MaxPlayers;

    public override string ToString()
    {
        return $"Room {RoomId} ({PlayerCount}/{MaxPlayers}) on {HostName} ({IpAddress})";
    }
}

public class RoomManager : SingletonMono<RoomManager>
{
    [Header("房间配置")]
    public int MaxPlayers = 4; // 房间最大人数

    private int _currentRoomId = 0;
    private readonly HashSet<int> _playerIds = new(); // 当前房间中的玩家 ID 集合

    public int CurrentRoomId => _currentRoomId;
    public HashSet<int> PlayerIds => _playerIds;
    public int PlayerCount => _playerIds.Count;
    public bool IsFull => PlayerCount >= MaxPlayers;
    public bool InRoom => _currentRoomId > 0;

    protected override void OnSingletonInit()
    {
        // 初始化时不在任何房间
        _currentRoomId = 0;
        _playerIds.Clear();
    }

    /// <summary>
    /// 创建新房间（主机）
    /// </summary>
    public bool CreateRoom(out int roomId)
    {
        if (InRoom)
        {
            Debug.LogWarning("Already in a room!");
            roomId = _currentRoomId;
            return false;
        }

        _currentRoomId = UnityEngine.Random.Range(1000, 9999); // 简易随机房间号
        _playerIds.Clear();

        // 主机自动加入
        AddPlayerToLocalRoom(0); // 假设主机 ID 为 0

        roomId = _currentRoomId;
        Debug.Log($"Created Room {roomId} as Host (Player 0)");
        return true;
    }

    /// <summary>
    /// 加入指定房间（客户端）
    /// </summary>
    public bool JoinRoom(int roomId, int playerId)
    {
        if (InRoom)
        {
            Debug.LogWarning("Already in a room!");
            return false;
        }

        if (playerId < 0 || playerId >= MaxPlayers)
        {
            Debug.LogError($"Player ID must be between 0 and {MaxPlayers - 1}");
            return false;
        }

        _currentRoomId = roomId;
        _playerIds.Clear();

        // 模拟：加入房间后，本地只知道自己的 ID
        // 实际多人游戏中，应从服务器获取完整玩家列表
        AddPlayerToLocalRoom(playerId);

        Debug.Log($"Joined Room {roomId} as Player {playerId}");
        return true;
    }

    /// <summary>
    /// 本地添加一个玩家到房间（用于模拟）
    /// </summary>
    public bool AddPlayerToLocalRoom(int playerId)
    {
        if (!InRoom)
        {
            Debug.LogWarning("Cannot add player: not in a room");
            return false;
        }

        if (_playerIds.Contains(playerId))
        {
            Debug.LogWarning($"Player {playerId} already in room");
            return false;
        }

        if (_playerIds.Count >= MaxPlayers)
        {
            Debug.LogWarning("Room is full!");
            return false;
        }

        _playerIds.Add(playerId);
        Debug.Log($"Player {playerId} joined room {_currentRoomId}. Total: {_playerIds.Count}/{MaxPlayers}");
        return true;
    }

    /// <summary>
    /// 移除玩家（模拟断开）
    /// </summary>
    public void RemovePlayer(int playerId)
    {
        if (_playerIds.Remove(playerId))
        {
            Debug.Log($"Player {playerId} left room {_currentRoomId}");
        }
    }

    /// <summary>
    /// 退出当前房间
    /// </summary>
    public void LeaveCurrentRoom()
    {
        if (!InRoom) return;

        Debug.Log($"Left Room {_currentRoomId}");
        _currentRoomId = 0;
        _playerIds.Clear();
    }

    /// <summary>
    /// 检查玩家是否在当前房间
    /// </summary>
    public bool HasPlayer(int playerId) => _playerIds.Contains(playerId);

    /// <summary>
    /// 获取所有玩家 ID（排序后）
    /// </summary>
    public List<int> GetSortedPlayerIds()
    {
        var list = new List<int>(_playerIds);
        list.Sort();
        return list;
    }
}