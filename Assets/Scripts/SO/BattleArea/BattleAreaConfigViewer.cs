using UnityEngine;

/// <summary>
/// 玩家出生点数据
/// </summary>
[System.Serializable]
public struct PlayerSpawnData
{
    public const int MaxPlayerCount = 4;

    [Tooltip("单机模式出生坐标")]
    public Vector2 SinglePlayerSpawnPos;

    [Tooltip("玩家 1 出生坐标")]
    public Vector2 Player1SpawnPos;

    [Tooltip("玩家 2 出生坐标")]
    public Vector2 Player2SpawnPos;

    [Tooltip("玩家 3 出生坐标")]
    public Vector2 Player3SpawnPos;

    [Tooltip("玩家 4 出生坐标")]
    public Vector2 Player4SpawnPos;

    /// <summary>
    /// 获取玩家出生位置。单机使用中心点；多人使用玩家索引对应的独立点。
    /// </summary>
    public readonly Vector2 GetPlayerSpawnPos(byte playerIndex, int totalPlayers)
    {
        if (totalPlayers <= 1)
            return SinglePlayerSpawnPos;

        if (playerIndex >= MaxPlayerCount)
            return SinglePlayerSpawnPos;

        return playerIndex switch
        {
            0 => Player1SpawnPos,
            1 => Player2SpawnPos,
            2 => Player3SpawnPos,
            3 => Player4SpawnPos,
            _ => Player1SpawnPos,
        };
    }

    public void SetPlayerSpawnPos(int playerIndex, Vector2 position)
    {
        switch (playerIndex)
        {
            case 0: Player1SpawnPos = position; break;
            case 1: Player2SpawnPos = position; break;
            case 2: Player3SpawnPos = position; break;
            case 3: Player4SpawnPos = position; break;
        }
    }

    public void SetSinglePlayerSpawnPos(Vector2 position)
    {
        SinglePlayerSpawnPos = position;
    }

    public static PlayerSpawnData Default => new PlayerSpawnData
    {
        SinglePlayerSpawnPos = new Vector2(0f, -2f),
        Player1SpawnPos = new Vector2(-1.5f, -2f),
        Player2SpawnPos = new Vector2(-0.5f, -2f),
        Player3SpawnPos = new Vector2(0.5f, -2f),
        Player4SpawnPos = new Vector2(1.5f, -2f),
    };
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

#if UNITY_EDITOR
    [Header("BattleAreaTool Scene 编辑")]
    [SerializeField] bool drawBattleAreaEditGrid = true;
    [SerializeField] bool battleAreaToolSnapToGrid = true;
    [Min(0.01f)]
    [SerializeField] float battleAreaToolSnapCellSize = 0.25f;

    public BattleAreaData EditorBattleAreaData => battleAreaData;
    public PlayerSpawnData EditorPlayerSpawnData => playerSpawnData;
    public DropItemCollectData EditorDropItemCollectData => dropItemCollectData;
    public bool DrawBattleAreaEditGrid => drawBattleAreaEditGrid;
    public bool BattleAreaToolSnapToGrid => battleAreaToolSnapToGrid;
    public float BattleAreaToolSnapCellSize => battleAreaToolSnapCellSize;

    public void SetEditorPlayerSpawnData(PlayerSpawnData data)
    {
        playerSpawnData = data;
        UnityEditor.EditorUtility.SetDirty(this);
    }

    public void SetEditorDropItemCollectData(DropItemCollectData data)
    {
        dropItemCollectData = data;
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

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

        Gizmos.color = Color.blue;
        for (byte i = 0; i < 4; i++)
        {
            Vector2 spawnPos = playerSpawnData.GetPlayerSpawnPos(i, 4);
            Gizmos.DrawSphere(new Vector3(spawnPos.x, spawnPos.y, 0), 0.1f);
        }

        Vector2 singleSpawnPos = playerSpawnData.GetPlayerSpawnPos(0, 1);
        Gizmos.color = new Color(1f, 0.75f, 0.2f);
        Gizmos.DrawWireSphere(new Vector3(singleSpawnPos.x, singleSpawnPos.y, 0), 0.14f);
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
