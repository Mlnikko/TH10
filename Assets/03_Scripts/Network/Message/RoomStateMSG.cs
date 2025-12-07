using Unity.Collections;

public struct RoomStateMSG : INetworkMessage
{
    public byte PlayerCount;
    // 可扩展：PlayerInfo[]，但 STG 简化可只传数量
    
    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte(PlayerCount);
    }
    public void Deserialize(in DataStreamReader reader)
    {
        PlayerCount = reader.ReadByte();
    }
}
