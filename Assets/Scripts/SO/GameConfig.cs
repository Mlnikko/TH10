using UnityEngine;
using System;

[Serializable]
public struct ColliderConfig
{
    [Tooltip("碰撞体是触发器")]
    public bool isTrigger;

    [Tooltip("碰撞体形状")]
    public E_ColliderShape shape;

    [Tooltip("碰撞体所在层")]
    public E_ColliderLayer layer;

    [Tooltip("碰撞掩码")]
    public E_ColliderLayer mask;

    [Tooltip("圆形碰撞半径")] [Min(0f)]
    public float radius;

    [Tooltip("矩形碰撞大小(全宽 x 全高)")] 
    public Vector2 boxSize;

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
