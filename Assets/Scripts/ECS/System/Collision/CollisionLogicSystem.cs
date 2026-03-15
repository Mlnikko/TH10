using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)] // 强制紧凑排列
public struct CollisionEvent
{
    // 1. 核心索引 (8 字节)
    public Entity EntityA;
    public Entity EntityB;

    // 2. 必要几何数据 (8 字节) - 用于计算特效位置/朝向/击退
    public float ContactX;
    public float ContactY;

#if UNITY_EDITOR
    public uint Frame; // 4 字节 - 仅编辑器用于调试
#endif
}

public static class CollisionEventBuffer
{
    const int MAX_EVENTS = 2048;
    static CollisionEvent[] _events = new CollisionEvent[MAX_EVENTS];
    static int _count = 0;

    public static int Count => _count;

    public static void Clear()
    {
        _count = 0;
    }

    public static bool Add(CollisionEvent evt)
    {
        if (_count >= MAX_EVENTS)
        {
            Logger.Error("[Collision] Event buffer overflow!", LogTag.Collision);
            return false;
        }
        _events[_count++] = evt;
        return true;
    }

    public static Span<CollisionEvent> GetEvents()
    {
        return new Span<CollisionEvent>(_events, 0, _count);
    }
}

public class CollisionLogicSystem : BaseSystem
{
    public override void OnLogicTick(uint frame)
    {
        var events = CollisionEventBuffer.GetEvents();
        if (events.Length == 0) return;

        var posComp = EntityManager.GetComponentSpan<CPosition>();
        var colComp = EntityManager.GetComponentSpan<CCollider>();
        var danmakuComp = EntityManager.GetComponentSpan<CDanmaku>(); // 包含 ConfigID, OwnerID
        var healthComp = EntityManager.GetComponentSpan<CHealth>();

       

       
    }
}
