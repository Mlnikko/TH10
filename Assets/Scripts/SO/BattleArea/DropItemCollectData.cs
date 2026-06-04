using System;
using UnityEngine;

/// <summary>
/// 道具吸收线配置：玩家进入指定世界 Y 线以上后，全场掉落物磁吸飞向玩家。
/// 与 <see cref="BattleAreaData"/> 分离，由 <see cref="BattleAreaConfig"/> 一并引用。
/// </summary>
[Serializable]
public struct DropItemCollectData
{
    [Tooltip("道具吸收线的世界 Y 坐标；玩家处于该高度以上时触发全场磁吸")]
    public float collectLineY;

    [Tooltip("道具被吸引飞向玩家的速度（世界单位/秒）")]
    [Min(0)]
    public float magnetSpeed;

    public static DropItemCollectData Default => new DropItemCollectData
    {
        collectLineY = 1.5f,
        magnetSpeed = 16f,
    };

    public float GetCollectLineY(in BattleAreaData area)
    {
        if (area.Height <= 0f)
            return collectLineY;

        return Mathf.Clamp(collectLineY, area.Bottom, area.Top);
    }

    public bool IsInCollectZone(float worldY, in BattleAreaData area)
        => worldY >= GetCollectLineY(area);

    public float ResolveMagnetSpeedPerSecond()
        => magnetSpeed > 0f ? magnetSpeed : 16f;
}
