using System;
using System.Collections.Generic;

public enum E_DanmakuType
{
    Custom,

}
public class DanmakuEntityManager
{
    static long eid = 0;
    static readonly Dictionary<long, DanmakuEntity> entities = new();

    public static DanmakuEntity CreateEntity()
    {
        DanmakuEntity e = new (eid);
        eid++;
        return e;
    }
    public static DanmakuEntity GetEntity(long entityId)
    {
        entities.TryGetValue(entityId, out DanmakuEntity entity);
        return entity;
    }
    public static void DestroyEntity(long entityId)
    {
        entities.Remove(entityId);
    }

    public static DanmakuEntity CreateCustomDanmakuEntity(DanmakuConfig config, DanmakuEmitterConfig emitterConfig)
    {
        var e = CreateEntity();
        e.AddComp(new Danmaku_TransformComponent(emitterConfig.PositionOffset, emitterConfig.StartRotation, emitterConfig.StartVelocity));

        switch(config.ColliderType)
        {
            case E_ColliderType.Circle:
                e.AddComp(new CircleColliderComponent(config.ColliderType, config.ColliderOffset, ((CircleDanmakuConfig)config).Radius));
                break;
            case E_ColliderType.Rect:
                e.AddComp(new RectColliderComponent(config.ColliderType, config.ColliderOffset, ((RectDanmakuConfig)config).Size));
                break;
        }     
        e.AddComp(new Danmaku_RenderComponent(config.Sprite,config.Color));
        return e;
    }
}

public class DanmakuEntity
{
    public long EntityId;
    readonly Dictionary<Type, object> components = new();

    public DanmakuEntity(long entityId)
    {
        EntityId = entityId;
    }

    public void AddComp<T>(T component) where T : class
    {
        components[typeof(T)] = component;
    }

    public void RemoveComp<T>() where T : class
    {
        components.Remove(typeof(T));
    }

    public T GetComp<T>(bool containBaseComp = false) where T : class
    {

        if (containBaseComp)
        {
            foreach (var comp in components.Values)
            {
                if (comp is T matchedComp)
                {
                    return matchedComp;
                }
            }
        }
        else
        {
            if (components.TryGetValue(typeof(T), out object comp))
            {
                return (T)comp;
            }
        }
        return default;
    }

    public bool HasComp<T>() where T : class
    {
        return components.ContainsKey(typeof(T));
    }
}
