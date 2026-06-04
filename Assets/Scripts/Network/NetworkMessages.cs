using Unity.Collections;
public enum MessageId : byte
{
    Heartbeat = 0,

    PingRequest,
    PingResponse,

    PlayerInput,
    SyncFrame,
    RoomState,
    JoinRequest,
    JoinResponse,

    StartGame,

    BattleReady,
    BattlePrepareCancel,
    BattleStart,

    /// <summary>房主广播：全员进入手动暂停。</summary>
    BattlePauseApply,
    /// <summary>房主广播：全员恢复战斗。</summary>
    BattlePauseResume,

    /// <summary>房主在暂停菜单选择返回房间，全员退出战斗回房。</summary>
    BattlePauseReturnToRoom,

    /// <summary>联机全员生命归零，强制结束战斗。</summary>
    BattleGameOver,

    /// <summary>联机关卡通关，全员进入通关暂停。</summary>
    BattleStageClear,

    /// <summary>房主发起联机重新开始本关。</summary>
    BattleRestart,

    /// <summary>房主离开房间，通知客户端一并退出。</summary>
    RoomHostLeave
}

/// <summary>
/// 消息接口，必须为值类型(struct)
/// </summary>
public interface INetworkMessage
{
    MessageId Id { get; }

    /// <summary>
    /// 封装消息
    /// </summary>
    /// <param name="writer"></param>
    void Serialize(ref DataStreamWriter writer);

    /// <summary>
    /// 解析消息
    /// </summary>
    /// <param name="reader"></param>
    void Deserialize(ref DataStreamReader reader);
}

#region Ping
public struct PingRequestMSG : INetworkMessage
{
    public readonly MessageId Id => MessageId.PingRequest;
    public uint timestamp; // 发送时的本地时间戳（毫秒）

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteUInt(timestamp);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        timestamp = reader.ReadUInt();
    }

}
public struct PingResponseMSG : INetworkMessage
{
    public readonly MessageId Id => MessageId.PingResponse;
    public uint timestamp; // 原始请求的时间戳

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteUInt(timestamp);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        timestamp = reader.ReadUInt();
    }
}
#endregion

#region 房间相关消息
public struct JoinResponseMSG : INetworkMessage
{
    public readonly MessageId Id => MessageId.JoinResponse;

    public bool accepted;
    public byte assignedPlayerIndex;
    public RoomInfo roomInfo;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)(accepted ? 1 : 0));
        if (!accepted)
            return;

        writer.WriteByte(assignedPlayerIndex);
        writer.WriteFixedString32(roomInfo.IpAddress);
        writer.WriteUShort(roomInfo.Port);
        writer.WriteByte(roomInfo.PlayerCount);
        writer.WriteByte(roomInfo.MaxPlayers);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        accepted = reader.ReadByte() != 0;
        if (!accepted)
            return;

        assignedPlayerIndex = reader.ReadByte();
        roomInfo.IpAddress = reader.ReadFixedString32().ToString();
        roomInfo.Port = reader.ReadUShort();
        roomInfo.PlayerCount = reader.ReadByte();
        roomInfo.MaxPlayers = reader.ReadByte();
    }
}
public struct JoinRequestMSG : INetworkMessage
{
    public readonly MessageId Id => MessageId.JoinRequest;

    public void Serialize(ref DataStreamWriter writer) { }

    public void Deserialize(ref DataStreamReader reader) { }
}
public struct RoomStateMSG : INetworkMessage
{
    public RoomInfo roomInfo;
    public readonly MessageId Id => MessageId.RoomState;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteFixedString32(roomInfo.IpAddress);
        writer.WriteUShort(roomInfo.Port);

        writer.WriteByte(roomInfo.PlayerCount);
        writer.WriteByte(roomInfo.MaxPlayers);
    }
    public void Deserialize(ref DataStreamReader reader)
    {
        roomInfo.IpAddress = reader.ReadFixedString32().ToString();
        roomInfo.Port = reader.ReadUShort();

        roomInfo.PlayerCount = reader.ReadByte();
        roomInfo.MaxPlayers = reader.ReadByte();
    }
}
public struct GameStartMSG : INetworkMessage
{
    public readonly MessageId Id => MessageId.StartGame;

    public void Deserialize(ref DataStreamReader reader)
    {

    }

    public void Serialize(ref DataStreamWriter writer)
    {

    }
}
#endregion

#region 战斗相关消息
public struct InputMSG : INetworkMessage
{
    public FrameInput frameInput;

    public readonly MessageId Id => MessageId.PlayerInput;

    public readonly void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteUInt(frameInput.frame);
        writer.WriteByte(frameInput.playerIndex);
        writer.WriteByte(frameInput.directionPacked);
        writer.WriteByte(frameInput.buttons);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        // 注意：如果数据不足，UTP 会返回默认值（0），不会崩溃
        // 所以建议在调用前校验 payload 长度（应为 7）
        frameInput.frame = reader.ReadUInt();
        frameInput.playerIndex = reader.ReadByte();
        frameInput.directionPacked = reader.ReadByte();
        frameInput.buttons = reader.ReadByte();
    }
}

public struct BattleReadyMSG : INetworkMessage
{
    public PlayerBattleData playerBattleData;

    public MessageId Id => MessageId.BattleReady;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte(playerBattleData.playerIndex);
        writer.WriteByte((byte)playerBattleData.characterId);
        writer.WriteByte((byte)playerBattleData.weaponId);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        playerBattleData.playerIndex = reader.ReadByte();
        playerBattleData.characterId = (E_Character)reader.ReadByte();
        playerBattleData.weaponId = (E_Weapon)reader.ReadByte();
    }
}

public struct BattlePrepareCancelMSG : INetworkMessage
{
    public byte playerIndex;

    public MessageId Id => MessageId.BattlePrepareCancel;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte(playerIndex);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        playerIndex = reader.ReadByte();
    }
}

public struct BattleStartMSG : INetworkMessage
{
    public PlayerBattleData[] playerDatas;
    public uint startFrame;         // 开始游戏的逻辑帧
    public uint randomSeed;        // 统一随机种子

    public MessageId Id => MessageId.BattleStart;

    public void Serialize(ref DataStreamWriter writer)
    {
        // 1. 写入玩家数量
        writer.WriteByte((byte)playerDatas.Length);

        // 2. 写入每个玩家的数据
        for (int i = 0; i < playerDatas.Length; i++)
        {
            var data = playerDatas[i];
            writer.WriteByte(data.playerIndex);
            writer.WriteByte((byte)data.characterId);
            writer.WriteByte((byte)data.weaponId);
        }

        // 3. 写入开始帧
        writer.WriteUInt(startFrame);

        // 4. 写入随机种子
        writer.WriteUInt(randomSeed);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        // 1. 读取玩家数量
        byte playerCount = reader.ReadByte();
        playerDatas = new PlayerBattleData[playerCount];

        // 2. 读取每个玩家的数据
        for (int i = 0; i < playerCount; i++)
        {
            playerDatas[i] = new PlayerBattleData
            {
                playerIndex = reader.ReadByte(),
                characterId = (E_Character)reader.ReadByte(),
                weaponId = (E_Weapon)reader.ReadByte()
            };
        }

        // 3. 读取开始帧
        startFrame = reader.ReadUInt();

        // 4. 读取随机种子
        randomSeed = reader.ReadUInt();
    }
}

public struct BattlePauseApplyMSG : INetworkMessage
{
    public MessageId Id => MessageId.BattlePauseApply;

    public void Serialize(ref DataStreamWriter writer) { }

    public void Deserialize(ref DataStreamReader reader) { }
}

public struct BattlePauseResumeMSG : INetworkMessage
{
    public MessageId Id => MessageId.BattlePauseResume;

    public void Serialize(ref DataStreamWriter writer) { }

    public void Deserialize(ref DataStreamReader reader) { }
}

public struct BattlePauseReturnToRoomMSG : INetworkMessage
{
    public MessageId Id => MessageId.BattlePauseReturnToRoom;

    public void Serialize(ref DataStreamWriter writer) { }

    public void Deserialize(ref DataStreamReader reader) { }
}

/// <summary>房主广播：全员生命归零，各端进入 Game Over 暂停。</summary>
public struct BattleGameOverMSG : INetworkMessage
{
    public MessageId Id => MessageId.BattleGameOver;

    public void Serialize(ref DataStreamWriter writer) { }

    public void Deserialize(ref DataStreamReader reader) { }
}

/// <summary>房主广播：关卡通关，各端进入通关暂停。</summary>
public struct BattleStageClearMSG : INetworkMessage
{
    public MessageId Id => MessageId.BattleStageClear;

    public void Serialize(ref DataStreamWriter writer) { }

    public void Deserialize(ref DataStreamReader reader) { }
}

/// <summary>房主广播：重新开始当前关卡（载荷与 <see cref="BattleStartMSG"/> 相同）。</summary>
public struct BattleRestartMSG : INetworkMessage
{
    public PlayerBattleData[] playerDatas;
    public uint startFrame;
    public uint randomSeed;

    public MessageId Id => MessageId.BattleRestart;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte((byte)playerDatas.Length);
        for (int i = 0; i < playerDatas.Length; i++)
        {
            var data = playerDatas[i];
            writer.WriteByte(data.playerIndex);
            writer.WriteByte((byte)data.characterId);
            writer.WriteByte((byte)data.weaponId);
        }

        writer.WriteUInt(startFrame);
        writer.WriteUInt(randomSeed);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        byte playerCount = reader.ReadByte();
        playerDatas = new PlayerBattleData[playerCount];
        for (int i = 0; i < playerCount; i++)
        {
            playerDatas[i] = new PlayerBattleData
            {
                playerIndex = reader.ReadByte(),
                characterId = (E_Character)reader.ReadByte(),
                weaponId = (E_Weapon)reader.ReadByte()
            };
        }

        startFrame = reader.ReadUInt();
        randomSeed = reader.ReadUInt();
    }
}

/// <summary>房主离开房间，客户端收到后自动退出房间界面。</summary>
public struct RoomHostLeaveMSG : INetworkMessage
{
    public MessageId Id => MessageId.RoomHostLeave;

    public void Serialize(ref DataStreamWriter writer) { }

    public void Deserialize(ref DataStreamReader reader) { }
}

#endregion