
using UnityEngine;

public interface IHittable
{
    /// <summary>
    /// 获取受击体的碰撞区域
    /// </summary>
    Rect GetColliderRect();

    /// <summary>
    /// 受击时的处理
    /// </summary>
    void OnHit(Danmaku danmaku);

    /// <summary>
    /// 获取受击体类型（可选，用于特殊处理）
    /// </summary>
    string GetHittableType();
}
