using Unity.Collections;

public struct StartGameMSG : INetworkMessage
{
    public int RoomId; // 用于校验
    public void Deserialize(in DataStreamReader reader)
    {
        RoomId = reader.ReadInt();
    }

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteInt(RoomId);
    }
}
