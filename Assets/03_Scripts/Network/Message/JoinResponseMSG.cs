using Unity.Collections;

public struct JoinResponseMSG : INetworkMessage
{
    public byte assignedPlayerIndex;
    public int currentPlayerCount;
    public bool success;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteByte(assignedPlayerIndex);
        writer.WriteInt(currentPlayerCount);
        //writer.Write
    }

    public void Deserialize(in DataStreamReader reader)
    {
        assignedPlayerIndex = reader.ReadByte();
        currentPlayerCount = reader.ReadInt();
        //success = reader.ReadBool();
    }
}