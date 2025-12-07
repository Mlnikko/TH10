using Unity.Collections;

public struct InputMSG : INetworkMessage
{
    public FrameInput input;
    public void Serialize(ref DataStreamWriter writer)
    {
        writer.WriteUInt(input.frame);
        writer.WriteByte(input.playerIndex);
        writer.WriteByte(input.directionPacked);
        writer.WriteByte(input.buttons);
    }

    public void Deserialize(in DataStreamReader reader)
    {
        // 注意：如果数据不足，UTP 会返回默认值（0），不会崩溃
        // 所以建议在调用前校验 payload 长度（应为 7）
        input.frame = reader.ReadUInt();
        input.playerIndex = reader.ReadByte();
        input.directionPacked = reader.ReadByte();
        input.buttons = reader.ReadByte();
    }
}