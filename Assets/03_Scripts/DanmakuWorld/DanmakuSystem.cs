using System.Collections.Generic;
using UnityEngine;

public class DanmakuSystem : SingletonMono<DanmakuSystem>
{
    public DanmakuArea danmakuArea;
    CollisionSystem collisionSystem;
    
    readonly Queue<Danmaku> danmaku_AddQueue = new();
    readonly List<Danmaku> danmaku_ActiveList = new();
    readonly Queue<Danmaku> danmaku_RemoveQueue = new();

    protected override void OnSingletonInit()
    {
        collisionSystem = new(DanmakuArea.battleRect, danmakuArea.GridRows, danmakuArea.GridColumns);
    }

    void Update()
    {
        collisionSystem.Update();     

        HandleAddDanmaku();

        HandleActiveDanmaku();

        HandleRemoveDanmaku();

        //SystemDebug();
    }

    public void AddDanmaku(Danmaku danmaku)
    {
        danmaku_AddQueue.Enqueue(danmaku);
    }
    public void RemoveDanmaku(Danmaku danmaku)
    {
        danmaku_RemoveQueue.Enqueue(danmaku);
    }

    public void AddEnemyCollider(ICollider collider)
    {
        CollisionSystem.AddCollider(collider);
    }

    void HandleAddDanmaku()
    {
        while (danmaku_AddQueue.Count > 0)
        {
            Danmaku danmaku = danmaku_AddQueue.Dequeue();
            CollisionSystem.AddCollider(danmaku.Collider);
            danmaku_ActiveList.Add(danmaku);            
        }
    }

    void HandleActiveDanmaku()
    {
        foreach (Danmaku danmaku in danmaku_ActiveList)
        {
            HandleDanmakuMove(danmaku);
        }
    }
    void HandleDanmakuMove(Danmaku danmaku)
    {
        if (danmakuArea.IsOutOfDanmakuRect(danmaku.Position))
        {
            RemoveDanmaku(danmaku);
            return;
        }
        if(danmaku.DanmakuType == E_DanmakuType.Homing)
        {
            
        }
        danmaku.Position += danmaku.Velocity * Time.deltaTime;
    }

    void HandleRemoveDanmaku()
    {
        while (danmaku_RemoveQueue.Count > 0) 
        {
            Danmaku danmaku = danmaku_RemoveQueue.Dequeue();      
            CollisionSystem.RemoveCollider(danmaku.Collider);
            danmaku_ActiveList.Remove(danmaku);
            danmaku.Release();
        }
    }

    void SystemDebug()
    {
        GameLogger.Debug($"»îÔ¾µ¯Ä»ÊýÁ¿£º{danmaku_ActiveList.Count}");
    }
}
