using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 表现更新器接口：每个表现类型实现自己的同步逻辑
/// </summary>
public interface IGameObjectUpdater
{
    void UpdateGameObject(in EntityManager em, Entity entity);
}

/// <summary>
/// 统一 GameObject桥接器
/// 职责：
/// 1. 注册/注销表现对象
/// 2. 提供 ID → GameObject 查询
/// 3. 驱动表现更新（通过回调或组件）
/// </summary>
public class GameObjectBridge
{
    readonly Dictionary<Entity, GameObject> _entityToGO;
    //readonly Dictionary<int, Entity> _goIDToEntity;

    const int MAX_QUERY_BUFFER = 8192;

    public GameObjectBridge()
    {
        _entityToGO = new Dictionary<Entity, GameObject>(1024);
        //_goIDToEntity = new Dictionary<int, Entity>(1024);
    }

    /// <summary>
    /// 注册一个 GameObject，并绑定更新器
    /// </summary>
    public void Link(Entity entity, GameObject go, IGameObjectUpdater updater, EntityManager em)
    {
        if (_entityToGO.ContainsKey(entity))
        {
            Logger.Warn($"Entity {entity.Index} already linked to a GameObject");
            return;
        }

        _entityToGO[entity] = go;
        //_goIDToEntity[go.GetInstanceID()] = entity;

        // 添加ECS组件
        em.AddComponent(entity, new CGameObjectLink
        {
            //GameObject = go,
            Updater = updater,
            IsDirty = true
        });

        //// 添加EntityLinkBehaviour（GO销毁时通知ECS）
        //var link = go.GetComponent<EntityLinkBehaviour>();
        //if (link == null) link = go.AddComponent<EntityLinkBehaviour>();
        //link.Initialize(entity, this);
    }

    public void Unlink(Entity entity, EntityManager em, bool returnToPool = true)
    {
        if (!_entityToGO.TryGetValue(entity, out var go))
        {
            return; // 已经解绑或从未绑定
        }

        // 1. 移除 ECS 组件
        if (em.IsValid(entity) && em.HasComponent<CGameObjectLink>(entity))
        {
            em.RemoveComponent<CGameObjectLink>(entity);
        }

        // 2. 清除映射
        //_goIDToEntity.Remove(go.GetInstanceID());
        _entityToGO.Remove(entity);

        // 4. 返回对象池
        if (returnToPool)
        {
            GameObjectPoolManager.Instance.Return(go);
        }
        else
        {
            //UnityEngine.Object.Destroy(go);
        }
    }

    /// <summary>
    /// 检查是否已关联
    /// </summary>
    public bool HasLink(Entity entity) => _entityToGO.ContainsKey(entity);

    /// <summary>
    /// 统一驱动所有表现更新
    /// 在 LateUpdate 中调用
    /// </summary>
    public void UpdateAllGameObjects(in EntityManager em)
    {
        // 使用 Span 栈分配，避免 GC
        Span<int> indices = stackalloc int[MAX_QUERY_BUFFER];

        // 查询所有带有 CGameObjectLink 的实体
        int count = em.GetEntities<CGameObjectLink>(indices);

        // 获取组件 Span 以便直接访问
        var linkSpan = em.GetComponentSpan<CGameObjectLink>();

        for (int i = 0; i < count; i++)
        {
            int index = indices[i];
            Entity entity = em.GetEntityByIndex(index);

            ref var link = ref linkSpan[index];

            // 快速路径：如果 Updater 为空，跳过并标记清理
            if (link.Updater == null)
            {
                // 这种情况通常不应该发生，如果发生了，说明数据不一致
                // 可以在这里添加 CDestroyTag 让逻辑层清理
                if (em.IsValid(entity))
                {
                   // em.AddComponent(entity, new CDestroyTag());
                }
                continue;
            }

            // 执行更新
            link.Updater.UpdateGameObject(em, entity);

            // 可选：如果 Updater 内部没有修改 IsDirty 的需求，这里可以统一置 false
            // 或者由 Updater 自己决定是否需要标记 isDirty
            link.IsDirty = false;
        }
    }
}

