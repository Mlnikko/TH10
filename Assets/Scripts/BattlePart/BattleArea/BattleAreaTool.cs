using System;
using UnityEngine;

/// <summary>
/// 战斗区域数据
/// </summary>
[Serializable]
public struct BattleAreaData
{
    // === 战斗区域 ===
    public float Width;          // 战斗区域宽度（单位：Unity 世界单位）
    public float Height;         // 战斗区域高度
    public Vector2 Center;       // 战斗区域中心点（通常为 (0,0)）

    // === 网格加速参数 ===
    [Min(1)]
    public int GridCellSize;     // 网格单元格尺寸（建议 64 或 128，正方形）

    // === 弹幕回收边界（外扩区域）===
    [Min(0)]
    public Vector2 DanmakuRecycleMargin; // 超出战斗区域多少距离后回收弹幕

    // === 辅助属性（只读，计算得来）===

    public float Left => Center.x - Width * 0.5f;
    public float Right => Center.x + Width * 0.5f;
    public float Bottom => Center.y - Height * 0.5f;
    public float Top => Center.y + Height * 0.5f;

    public Rect BattleRect => new Rect(Left, Bottom, Width, Height);

    // 回收区域边界（用于销毁远距离弹幕）
    public float RecycleLeft => Left - DanmakuRecycleMargin.x;
    public float RecycleRight => Right + DanmakuRecycleMargin.x;
    public float RecycleBottom => Bottom - DanmakuRecycleMargin.y;
    public float RecycleTop => Top + DanmakuRecycleMargin.y;

    // 用于 DeterministicGrid 的世界原点（左下角 - 边距）
    public Vector2 GridWorldOrigin => new Vector2(
        RecycleLeft - 50f,   // 额外安全边距
        RecycleBottom - 50f
    );

    // 总覆盖宽度/高度（用于计算网格大小）
    public float TotalWidth => Width + DanmakuRecycleMargin.x * 2f + 100f;
    public float TotalHeight => Height + DanmakuRecycleMargin.y * 2f + 100f;

    // 网格维度（向上取整）
    public int GridColumns => Mathf.CeilToInt(TotalWidth / GridCellSize);
    public int GridRows => Mathf.CeilToInt(TotalHeight / GridCellSize);

    public BattleAreaData(float width, float height, Vector2 center, int cellSize = 64, Vector2 recycleMargin = default)
    {
        Width = width;
        Height = height;
        Center = center;
        GridCellSize = cellSize;
        DanmakuRecycleMargin = recycleMargin == default ? new Vector2(100, 100) : recycleMargin;
    }

    // 默认构造（避免未初始化）
    public static BattleAreaData Default => new BattleAreaData(1280, 720, Vector2.zero, 64, new Vector2(100, 100));

    public bool IsPointInRecycleArea(float x , float y)
    {
        return x >= RecycleLeft && x <= RecycleRight && y >= RecycleBottom && y <= RecycleTop;
    }
}

/// <summary>
/// 玩家出生点数据
/// </summary>
[Serializable]
public struct PlayerSpawnData
{
    public Vector2 SpawnRootPos;
    public float HOffsetPerPlayer;

    /// <summary>
    /// 获取玩家出生位置，需指定总玩家数以保证对称布局。
    /// </summary>
    public readonly Vector2 GetPlayerSpawnPos(byte playerIndex, int totalPlayers)
    {
        if (totalPlayers <= 0) totalPlayers = 1;
        if (totalPlayers > 4) totalPlayers = 4;
        if (playerIndex >= totalPlayers)
            return SpawnRootPos; // 容错

        switch(totalPlayers)
        {
            case 1:
                return SpawnRootPos;
            case 2:
                return SpawnRootPos + (playerIndex == 0 ? Vector2.left : Vector2.right) * HOffsetPerPlayer;
            case 3:
                return playerIndex switch
                {
                    0 => SpawnRootPos + Vector2.left * HOffsetPerPlayer,
                    1 => SpawnRootPos,
                    2 => SpawnRootPos + Vector2.right * HOffsetPerPlayer,
                    _ => SpawnRootPos
                };
            case 4:
                {
                    // 对称四点：-1.5, -0.5, +0.5, +1.5 倍偏移 → 中心仍在 SpawnRootPos
                    float x = (playerIndex - 1.5f) * HOffsetPerPlayer;
                    return new Vector2(SpawnRootPos.x + x, SpawnRootPos.y);
                }
            default:
                return SpawnRootPos;
        }
    }
}

public class BattleAreaTool : MonoBehaviour
{
    [Header("配置引用")]
    public BattleAreaConfig battleAreaConfig;

    [Header("战斗区域数据")]
    [SerializeField] BattleAreaData battleAreaData;

    [Header("玩家出生点数据")]
    [SerializeField] PlayerSpawnData playerSpawnData;

    public void LoadBattleAreaData()
    {
        if(battleAreaConfig == null)
        {
            Logger.Error("BattleAreaConfig is not assigned!");
            return;
        }
        battleAreaData = battleAreaConfig.battleAreaData;
        playerSpawnData = battleAreaConfig.playerSpawnData;
    }

    public void SaveBattleAreaData()
    {
        if (battleAreaConfig != null)
        {
            battleAreaConfig.battleAreaData = battleAreaData;   
            battleAreaConfig.playerSpawnData = playerSpawnData;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (battleAreaConfig == null) return;

        // === 战斗区域（绿色）===
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(battleAreaData.Center, new Vector3(battleAreaData.Width, battleAreaData.Height, 0));

        // === 回收边界（红色）===
        Gizmos.color = Color.red;
        Vector3 recycleSize = new Vector3(
            battleAreaData.Width + battleAreaData.DanmakuRecycleMargin.x * 2f,
            battleAreaData.Height + battleAreaData.DanmakuRecycleMargin.y * 2f,
            0
        );
        Gizmos.DrawWireCube(battleAreaData.Center, recycleSize);

        // === 特殊基准点：SpawnRootPos（黄色，更大）===
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(playerSpawnData.SpawnRootPos.x, playerSpawnData.SpawnRootPos.y, 0), 0.2f);

        // === 玩家出生点预览（蓝色小球，默认按 4 人）===
        Gizmos.color = Color.blue;
        for (byte i = 0; i < 4; i++)
        {
            Vector2 spawnPos = playerSpawnData.GetPlayerSpawnPos(i, 4); // 明确按 4 人预览
            Gizmos.DrawSphere(new Vector3(spawnPos.x, spawnPos.y, 0), 0.1f);
        }
    }
}
