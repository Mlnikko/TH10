using System;
using UnityEngine;

/// <summary>
/// 道具吸收线配置：玩家进入战斗区上方区域后，全场掉落物磁吸飞向玩家。
/// 与 <see cref="BattleAreaData"/> 分离，由 <see cref="BattleAreaConfig"/> 一并引用。
/// </summary>
[Serializable]
public struct DropItemCollectData
{
    [Tooltip("自战斗区上沿向下的高度（与战斗区 Width/Height 同单位）；0 表示默认取战斗区高度的 15%")]
    [Min(0)]
    public float collectZoneHeight;

    [Tooltip("道具被吸引飞向玩家的速度（世界单位/秒）")]
    [Min(0)]
    public float magnetSpeed;

    [Tooltip("道具与玩家距离小于此值时判定拾取（世界单位）")]
    [Min(0)]
    public float magnetPickupRadius;

    public static DropItemCollectData Default => new DropItemCollectData
    {
        collectZoneHeight = 0f,
        magnetSpeed = 16f,
        magnetPickupRadius = 0.08f,
    };

    public float ResolveCollectZoneHeight(float battleAreaHeight)
    {
        float fallback = battleAreaHeight > 0f ? battleAreaHeight * 0.15f : 0.8f;
        if (collectZoneHeight <= 0f)
            return fallback;

        return battleAreaHeight > 0f
            ? Mathf.Min(collectZoneHeight, battleAreaHeight)
            : collectZoneHeight;
    }

    public float GetCollectLineY(in BattleAreaData area)
        => area.Top - ResolveCollectZoneHeight(area.Height);

    public bool IsInCollectZone(float worldY, in BattleAreaData area)
        => worldY >= GetCollectLineY(area);

    public float ResolveMagnetSpeedPerSecond()
        => magnetSpeed > 0f ? magnetSpeed : 16f;

    public float ResolveMagnetPickupRadius()
        => magnetPickupRadius > 0f ? magnetPickupRadius : 0.08f;
}
