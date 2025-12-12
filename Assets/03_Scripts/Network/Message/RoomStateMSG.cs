using Unity.Collections;

public struct RoomStateMSG : INetworkMessage
{
    public byte playerCount;
    public byte maxPlayers;
    // 可扩展：PlayerInfo[]，但 STG 简化可只传数量

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte(playerCount);
        writer.WriteByte(maxPlayers);
    }
    public void Deserialize(in DataStreamReader reader)
    {
        playerCount = reader.ReadByte();
        maxPlayers = reader.ReadByte();
    }
}
