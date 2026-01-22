using System;
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
public static class GameObjectBridge
{
    static readonly GameObject[] _gameObjects = new GameObject[EntityManager.MAX_ENTITIES];
    static readonly IGameObjectUpdater[] _updaters = new IGameObjectUpdater[EntityManager.MAX_ENTITIES];
    static int _nextId = 1;

    /// <summary>
    /// 注册一个 GameObject，并绑定更新器
    /// </summary>
    public static int Register(GameObject go, IGameObjectUpdater updater)
    {
        if (_nextId >= EntityManager.MAX_ENTITIES)
            throw new InvalidOperationException("Presentation ID pool exhausted");

        int id = _nextId++;
        _gameObjects[id] = go;
        _updaters[id] = updater;
        return id;
    }

    public static void Unregister(int id)
    {
        if (id > 0 && id < _nextId)
        {
            _gameObjects[id] = null;
            _updaters[id] = null;
        }
    }

    /// <summary>
    /// 获取 GameObject（仅用于调试或特殊逻辑）
    /// </summary>
    public static GameObject GetGameObject(int id) =>
        (id > 0 && id < _nextId) ? _gameObjects[id] : null;

    /// <summary>
    /// 统一驱动所有表现更新
    /// 在 LateUpdate 中调用
    /// </summary>
    public static void UpdateAllGameObjects(in EntityManager em)
    {
        Span<int> indices = stackalloc int[1024];
        int count = em.GetEntities<CGameObjectLink>(indices);

        for (int i = 0; i < count; i++)
        {
            int index = indices[i];
            Entity entity = em.GetEntityByIndex(index);
            if (!em.IsValid(entity)) continue; // ← 安全校验

            int id = em.GetComponentSpan<CGameObjectLink>()[index].gameObjectId;
            _updaters[id]?.UpdateGameObject(em, entity);
        }
    }
}

