using System.Collections.Generic;
using UnityEngine;

public class Danmaku
{
    public GameObject GameObject;
    public DanmakuEntity Entity;
    ObjectPool<Danmaku> danmakuPool;

    public Danmaku(GameObject gameObject, DanmakuEntity entity, ObjectPool<Danmaku> pool)
    {
        GameObject = gameObject;
        Entity = entity;
        danmakuPool = pool;
    }

    public void Release()
    {
        if (danmakuPool == null) return;
        danmakuPool.Release(this);
    }
}

public class DanmakuSystem : SingletonMono<DanmakuSystem>
{
    public DanmakuArea area;

    Quadtree<Danmaku> danmakuQuadtree;

    List<IHittable> allHittables = new();

    readonly Queue<Danmaku> danmaku_AddQueue = new();
    readonly List<Danmaku> danmaku_ActiveList = new();
    readonly Queue<Danmaku> danmaku_RemoveQueue = new();

    void Start()
    {
        Rect rect = new(BattleRect.Center.x, BattleRect.Center.y, BattleRect.Size.x, BattleRect.Size.y);
        danmakuQuadtree = new(0, 4, 8, rect);
    }

    void Update()
    {
        HandleAddDanmaku();

        HandleActiveDanmaku();

        HandleRemoveDanmaku();

        //SystemDebug();
    }

    public void AddHittable(IHittable hittable)
    {
        if (!allHittables.Contains(hittable))
        {
            allHittables.Add(hittable);
        }
    }
    public void RemoveHittable(IHittable hittable) 
    {
        allHittables.Remove(hittable);
    }

    void InsertDanmakuToQuadtree(Danmaku danmaku)
    {
        var transformComp = danmaku.Entity.GetComp<Danmaku_TransformComponent>();
        var colliderComp = danmaku.Entity.GetComp<ColliderComponent>(true);

        switch (colliderComp.ColliderType)
        {
            case E_ColliderType.Circle:
                break;

            case E_ColliderType.Rect:
                var rectColliderComp = (RectColliderComponent)colliderComp;
                Rect danmakuRect = new(
                    transformComp.Position.x - rectColliderComp.Size.x / 2,
                    transformComp.Position.y - rectColliderComp.Size.y / 2,
                    rectColliderComp.Size.x,
                    rectColliderComp.Size.y
                    );

                danmakuQuadtree.Insert(new QuadtreeObject<Danmaku>(danmaku, danmakuRect));
                break;
        }
    }
    

    bool IsColliding(Rect a, Rect b)
    {
        return a.x < b.x + b.width && a.x + a.width > b.x &&
               a.y < b.y + b.height && a.y + a.height > b.y;
    }

    void SystemDebug()
    {
        GameLogger.Debug($"活跃弹幕数量：{danmaku_ActiveList.Count}");
    }

    void HandleAddDanmaku()
    {
        while (danmaku_AddQueue.Count > 0)
        {
            danmaku_ActiveList.Add(danmaku_AddQueue.Dequeue());
        }
    }

    void HandleActiveDanmaku()
    {
        danmakuQuadtree.Clear();      

        foreach (Danmaku danmaku in danmaku_ActiveList)
        {
            InsertDanmakuToQuadtree(danmaku);
            HandleDanmakuMove(danmaku);          
        }
        CheckAllCollisions();
    }
    void CheckAllCollisions()
    {
        foreach (IHittable hittable in allHittables)
        {
            if (hittable == null) continue;

            Rect hittableRect = hittable.GetColliderRect();

            List<Danmaku> potentialCollisions = danmakuQuadtree.Retrieve(hittableRect);

            foreach (Danmaku danmaku in potentialCollisions)
            {
                if (danmaku == null) continue;

                var transformComp = danmaku.Entity.GetComp<Danmaku_TransformComponent>();
                var colliderComp = danmaku.Entity.GetComp<ColliderComponent>();

                switch (colliderComp.ColliderType)
                {
                    case E_ColliderType.Circle:
                        break;

                    case E_ColliderType.Rect:
                        var rectColliderComp = (RectColliderComponent)colliderComp;
                        Rect bulletRect = new(
                           transformComp.Position.x - rectColliderComp.Size.x / 2,
                           transformComp.Position.y - rectColliderComp.Size.y / 2,
                           rectColliderComp.Size.x,
                           rectColliderComp.Size.y
                           );
                        if (IsColliding(hittableRect, bulletRect))
                        {
                            hittable.OnHit(danmaku);
                            break; // 一次只处理一个碰撞
                        }
                        break;
                }
                ;
            }
        }
    }

    void HandleRemoveDanmaku()
    {
        while (danmaku_RemoveQueue.Count > 0) 
        {
            Danmaku danmaku = danmaku_RemoveQueue.Dequeue();
            danmaku.Release();
            danmaku_ActiveList.Remove(danmaku);
        }
    }

    void HandleDanmakuMove(Danmaku danmaku)
    {
        GameObject danmakuGo = danmaku.GameObject;
        var transformComponent = danmaku.Entity.GetComp<Danmaku_TransformComponent>();
        var colliderComponent = danmaku.Entity.GetComp<ColliderComponent>(true);

        if (area.IsOutOfBounds(transformComponent.Position + colliderComponent.Offset))
        {
            RemoveDanmaku(danmaku);
            return;
        }

        transformComponent.Position += transformComponent.Velocity * Time.deltaTime;
        danmakuGo.transform.position = transformComponent.Position;
    }

    public void AddDanmaku(Danmaku danmaku)
    {
        danmaku_AddQueue.Enqueue(danmaku);
    }
    public void RemoveDanmaku(Danmaku danmaku)
    {
        danmaku_RemoveQueue.Enqueue(danmaku);
    }
}
