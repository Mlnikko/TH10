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

public class BattleAreaConfigViewer : MonoBehaviour
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
