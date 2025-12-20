using Unity.Collections;
public enum MessageId : byte
{
    Heartbeat = 0,
    PlayerInput,
    SyncFrame,
    RoomState,
    JoinRequest,
    JoinResponse,

    StartGame,

    PlayerBattleDataConfirmed,

    BattleReady
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

#region 房间相关消息
public struct JoinResponseMSG : INetworkMessage
{
    public readonly MessageId Id => MessageId.JoinResponse;

    public byte assignedPlayerIndex;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte(assignedPlayerIndex);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        assignedPlayerIndex = reader.ReadByte();
    }
}
public struct JoinRequestMSG : INetworkMessage
{
    public string playerName;

    public readonly MessageId Id => MessageId.JoinRequest;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteFixedString32(playerName);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        playerName = reader.ReadFixedString32().ToString();
    }
}
public struct RoomStateMSG : INetworkMessage
{
    public RoomInfo roomInfo;
    public readonly MessageId Id => MessageId.RoomState;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteInt(roomInfo.RoomId);
        writer.WriteFixedString32(roomInfo.HostName);
        writer.WriteFixedString32(roomInfo.IpAddress);
        writer.WriteUShort(roomInfo.Port);

        writer.WriteByte(roomInfo.PlayerCount);
        writer.WriteByte(roomInfo.MaxPlayers);
    }
    public void Deserialize(ref DataStreamReader reader)
    {
        roomInfo.RoomId = reader.ReadInt();
        roomInfo.HostName = reader.ReadFixedString32().ToString();
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
    public FrameInput input;

    public readonly MessageId Id => MessageId.PlayerInput;

    public readonly void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteUInt(input.frame);
        writer.WriteByte(input.playerIndex);
        writer.WriteByte(input.directionPacked);
        writer.WriteByte(input.buttons);
    }

    public void Deserialize(ref DataStreamReader reader)
    {
        // 注意：如果数据不足，UTP 会返回默认值（0），不会崩溃
        // 所以建议在调用前校验 payload 长度（应为 7）
        input.frame = reader.ReadUInt();
        input.playerIndex = reader.ReadByte();
        input.directionPacked = reader.ReadByte();
        input.buttons = reader.ReadByte();
    }
}


public struct PlayerBattleDataConfirmedMSG : INetworkMessage
{
    public PlayerBattleData playerBattleData;

    public MessageId Id => MessageId.PlayerBattleDataConfirmed;

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

public struct BattleReadyMSG : INetworkMessage
{
    public PlayerBattleData[] allPlayerBattleDatas;
    public uint startFrame;         // 开始游戏的逻辑帧
    public uint randomSeed;        // 统一随机种子

    public MessageId Id => MessageId.BattleReady;

    public void Serialize(ref DataStreamWriter writer)
    {
        // 1. 写入玩家数量
        writer.WriteByte((byte)allPlayerBattleDatas.Length);

        // 2. 写入每个玩家的数据
        for (int i = 0; i < allPlayerBattleDatas.Length; i++)
        {
            var data = allPlayerBattleDatas[i];
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
        allPlayerBattleDatas = new PlayerBattleData[playerCount];

        // 2. 读取每个玩家的数据
        for (int i = 0; i < playerCount; i++)
        {
            allPlayerBattleDatas[i] = new PlayerBattleData
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

#endregion