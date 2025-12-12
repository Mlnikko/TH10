using Unity.Collections;

public struct JoinRequestMSG : INetworkMessage
{
    public string playerName;

    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteFixedString32(playerName);
    }

    public void Deserialize(in DataStreamReader reader)
    {
        playerName = reader.ReadFixedString32().ToString();
    }
}
