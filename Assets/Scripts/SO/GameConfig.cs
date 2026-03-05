using UnityEngine;
using System;

[Serializable]
public struct ColliderConfig
{
    [Tooltip("碰撞体类型")]
    public E_ColliderType type;

    [Tooltip("碰撞体所在层")]
    public E_ColliderLayer layer;

    [Tooltip("碰撞掩码")]
    public E_ColliderLayer mask;

    // Circle
    [Tooltip("圆形碰撞体半径")]
    [Min(0f)]
    public float radius;

    // Rect
    [Tooltip("矩形碰撞体")] 
    public Vector2 boxSize;

    // 相对偏移
    [Tooltip("碰撞体相对偏移")]
    public Vector2 offset;
}


public abstract class GameConfig : ScriptableObject
{
    [HideInInspector]
    public string configId = string.Empty;

    [NonSerialized]
    public int configIndex = -1;
}
