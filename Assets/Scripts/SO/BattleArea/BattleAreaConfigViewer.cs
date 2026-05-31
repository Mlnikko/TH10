using UnityEngine;

/// <summary>
/// 玩家出生点数据
/// </summary>
[System.Serializable]
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
            return SpawnRootPos;

        switch (totalPlayers)
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
                float x = (playerIndex - 1.5f) * HOffsetPerPlayer;
                return new Vector2(SpawnRootPos.x + x, SpawnRootPos.y);
            }
            default:
                return SpawnRootPos;
        }
    }
}

/// <summary>
/// 战斗区 Scene 编辑（Gizmo）；背景预览已移至 <see cref="StageTimelineConfigViewer"/>。
/// </summary>
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
    [Tooltip("按 GridCellSize 在 GO 回收区内绘制碰撞加速网格（与 DeterministicGrid 一致）")]
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
        if (battleAreaConfig == null)
            return;

        battleAreaConfig.battleAreaData = battleAreaData;
        battleAreaConfig.playerSpawnData = playerSpawnData;
        battleAreaConfig.dropItemCollectData = dropItemCollectData;
    }

    void OnDrawGizmosSelected()
    {
        if (battleAreaConfig == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(battleAreaData.Center, new Vector3(battleAreaData.Width, battleAreaData.Height, 0));

        Gizmos.color = Color.red;
        Vector3 recycleSize = new Vector3(
            battleAreaData.Width + battleAreaData.GO_RecycleMargin.x * 2f,
            battleAreaData.Height + battleAreaData.GO_RecycleMargin.y * 2f,
            0
        );
        Gizmos.DrawWireCube(battleAreaData.Center, recycleSize);

        if (drawCollisionGrid)
            DrawCollisionGridGizmo(in battleAreaData);

        if (battleAreaData.Height > 0f)
        {
            float lineY = dropItemCollectData.GetCollectLineY(in battleAreaData);
            Gizmos.color = Color.cyan;
            Vector3 lineLeft = new Vector3(battleAreaData.Left, lineY, 0f);
            Vector3 lineRight = new Vector3(battleAreaData.Right, lineY, 0f);
            Gizmos.DrawLine(lineLeft, lineRight);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(new Vector3(playerSpawnData.SpawnRootPos.x, playerSpawnData.SpawnRootPos.y, 0), 0.2f);

        Gizmos.color = Color.blue;
        for (byte i = 0; i < 4; i++)
        {
            Vector2 spawnPos = playerSpawnData.GetPlayerSpawnPos(i, 4);
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
