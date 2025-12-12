using Unity.Collections;

public enum MessageId : byte
{
    Heartbeat = 0,
    PlayerInput = 1,
    SyncFrame = 2,
    JoinRequest = 3,
    JoinResponse = 4,
}

/// <summary>
/// 消息接口，必须为值类型(struct)
/// </summary>
public interface INetworkMessage
{
    void Serialize(ref DataStreamWriter writer);
    void Deserialize(in DataStreamReader reader);
}