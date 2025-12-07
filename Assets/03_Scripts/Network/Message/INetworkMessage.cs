using Unity.Collections;

/// <summary>
/// 消息接口，必须为值类型(struct)
/// </summary>
public interface INetworkMessage
{
    void Serialize(ref DataStreamWriter writer);
    void Deserialize(in DataStreamReader reader);
}