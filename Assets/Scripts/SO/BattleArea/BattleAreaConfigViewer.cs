using System;
using UnityEngine;

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

public class BattleAreaConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => battleAreaConfig != null;

    [Header("配置引用")]
    public BattleAreaConfig battleAreaConfig;

    [Header("战斗区域数据")]
    [SerializeField] BattleAreaData battleAreaData;

    [Header("玩家出生点数据")]
    [SerializeField] PlayerSpawnData playerSpawnData;

    [Header("道具吸收")]
    [SerializeField] DropItemCollectData dropItemCollectData;

    [Header("Scene 可视化")]
    [Tooltip("按 GridCellSize 绘制碰撞加速网格（与 DeterministicGrid 一致），便于调节格子大小")]
    [SerializeField] bool drawCollisionGrid = true;

    public void LoadBattleAreaData() => LoadFromConfig();

    public override void LoadFromConfig()
    {
        if (battleAreaConfig == null)
        {
            Logger.Error("BattleAreaConfig is not assigned!");
            return;
        }
        battleAreaData = battleAreaConfig.battleAreaData;
        playerSpawnData = battleAreaConfig.playerSpawnData;
        dropItemCollectData = battleAreaConfig.dropItemCollectData;
    }

    public void SaveBattleAreaData()
    {
        if (battleAreaConfig != null)
        {
            battleAreaConfig.battleAreaData = battleAreaData;   
            battleAreaConfig.playerSpawnData = playerSpawnData;
            battleAreaConfig.dropItemCollectData = dropItemCollectData;
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
            battleAreaData.Width + battleAreaData.GO_RecycleMargin.x * 2f,
            battleAreaData.Height + battleAreaData.GO_RecycleMargin.y * 2f,
            0
        );
        Gizmos.DrawWireCube(battleAreaData.Center, recycleSize);

        if (drawCollisionGrid)
            DrawCollisionGridGizmo(in battleAreaData);

        // === 道具吸收线（青色）===
        if (battleAreaData.Height > 0f)
        {
            float lineY = dropItemCollectData.GetCollectLineY(in battleAreaData);
            Gizmos.color = Color.cyan;
            Vector3 lineLeft = new Vector3(battleAreaData.Left, lineY, 0f);
            Vector3 lineRight = new Vector3(battleAreaData.Right, lineY, 0f);
            Gizmos.DrawLine(lineLeft, lineRight);
        }

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

    void DrawCollisionGridGizmo(in BattleAreaData area)
    {
        float cell = area.GridCellSize;
        if (cell < 0.01f)
            return;
        float originX = area.GridWorldOrigin.x;
        float originY = area.GridWorldOrigin.y;
        float maxX = area.GridMaxX;
        float maxY = area.GridMaxY;
        int cols = area.GridColumns;
        int rows = area.GridRows;
        if (cols <= 0 || rows <= 0)
            return;

        Gizmos.color = new Color(0.45f, 0.85f, 0.45f, 0.45f);

        for (int c = 0; c <= cols; c++)
        {
            float x = originX + c * cell;
            Gizmos.DrawLine(new Vector3(x, originY, 0f), new Vector3(x, maxY, 0f));
        }

        for (int r = 0; r <= rows; r++)
        {
            float y = originY + r * cell;
            Gizmos.DrawLine(new Vector3(originX, y, 0f), new Vector3(maxX, y, 0f));
        }
    }
}
