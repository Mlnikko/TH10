using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleAreaConfigViewer))]
public class BattleAreaConfigEditor : Editor
{
    const string ConfigField = "battleAreaConfig";
    const string BattleAreaDataField = "battleAreaData";
    const string DrawCollisionGridField = "drawCollisionGrid";
    const float PlayerSpawnHandleSize = 0.1f;
    const float CollectLineHandleSize = 0.08f;

    public override void OnInspectorGUI()
    {
        var viewer = (BattleAreaConfigViewer)target;

        serializedObject.Update();

        var previousConfig = viewer.battleAreaConfig;

        var configRef = serializedObject.FindProperty(ConfigField);
        bool configRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(configRef);

        DrawBattleAreaDataSection(
            serializedObject.FindProperty(BattleAreaDataField),
            serializedObject.FindProperty(DrawCollisionGridField));

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            ConfigField,
            BattleAreaDataField,
            DrawCollisionGridField);

        serializedObject.ApplyModifiedProperties();

        ConfigViewerEditorUI.SyncViewerOnConfigReferenceChanged(
            viewer,
            previousConfig,
            viewer.battleAreaConfig,
            serializedObject,
            configRefChanged);

        ConfigViewerEditorUI.DrawSeparator();

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.battleAreaConfig, "BattleAreaConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint(
            "切换配置文件或双击进入预制体编辑后，会自动从 BattleAreaConfig 同步战斗区/出生点等参数。");
        ConfigViewerEditorUI.DrawSaveButton(
            viewer.battleAreaConfig,
            () =>
            {
                viewer.SaveBattleAreaData();
                Logger.Info($"战斗区域配置已更新：{viewer.battleAreaConfig.name}");
            },
            "BattleAreaConfig");
    }

    static void DrawBattleAreaDataSection(SerializedProperty area, SerializedProperty drawCollisionGrid)
    {
        if (area == null)
            return;

        EditorGUILayout.LabelField("战斗区域", EditorStyles.boldLabel);
        DrawRelative(area, nameof(BattleAreaData.Width));
        DrawRelative(area, nameof(BattleAreaData.Height));
        DrawRelative(area, nameof(BattleAreaData.Center));
        DrawRelative(area, nameof(BattleAreaData.GO_RecycleMargin));

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("碰撞网格", EditorStyles.boldLabel);
        DrawRelative(area, nameof(BattleAreaData.GridCellSize), new GUIContent("网格单元尺寸"));
        if (drawCollisionGrid != null)
            EditorGUILayout.PropertyField(drawCollisionGrid, new GUIContent("绘制碰撞网格预览"));

        EditorGUILayout.Space(6f);
    }

    static void DrawRelative(SerializedProperty parent, string relativeName, GUIContent label = null)
    {
        var prop = parent.FindPropertyRelative(relativeName);
        if (prop == null)
            return;

        if (label != null)
            EditorGUILayout.PropertyField(prop, label);
        else
            EditorGUILayout.PropertyField(prop);
    }

    void OnSceneGUI()
    {
        if (target is not BattleAreaConfigViewer viewer || viewer.battleAreaConfig == null)
            return;

        var area = viewer.EditorBattleAreaData;
        if (area.Width <= 0f || area.Height <= 0f)
            return;

        var spawn = viewer.EditorPlayerSpawnData;
        var collect = viewer.EditorDropItemCollectData;

        if (viewer.DrawBattleAreaEditGrid)
            StageTimelinePathEditSnap.DrawGridForBattleArea(area, viewer.BattleAreaToolSnapCellSize);

        EditorGUI.BeginChangeCheck();
        DrawSinglePlayerSpawnHandle(viewer, area, ref spawn);
        DrawPlayerSpawnHandles(viewer, area, ref spawn);
        DrawDropItemCollectLineHandle(viewer, area, ref collect);
        if (!EditorGUI.EndChangeCheck())
            return;

        Undo.RecordObject(viewer, "Edit Battle Area Preview Points");
        viewer.SetEditorPlayerSpawnData(spawn);
        viewer.SetEditorDropItemCollectData(collect);
        SceneView.RepaintAll();
    }

    static void DrawSinglePlayerSpawnHandle(BattleAreaConfigViewer viewer, BattleAreaData area, ref PlayerSpawnData spawn)
    {
        Vector2 current = spawn.GetPlayerSpawnPos(0, 1);
        Vector2 next = DrawSpawnPositionHandle(
            current,
            new Color(1f, 0.75f, 0.2f),
            "单机",
            PlayerSpawnHandleSize * 1.15f,
            viewer,
            area);

        if (Approximately(next, current))
            return;

        spawn.SetSinglePlayerSpawnPos(ClampToBattleArea(next, area));
    }

    static void DrawPlayerSpawnHandles(BattleAreaConfigViewer viewer, BattleAreaData area, ref PlayerSpawnData spawn)
    {
        for (byte i = 0; i < PlayerSpawnData.MaxPlayerCount; i++)
            DrawMultiplayerSpawnHandle(i, viewer, area, ref spawn);
    }

    static void DrawMultiplayerSpawnHandle(
        byte playerIndex,
        BattleAreaConfigViewer viewer,
        BattleAreaData area,
        ref PlayerSpawnData spawn)
    {
        Vector2 current = spawn.GetPlayerSpawnPos(playerIndex, 4);
        Vector2 next = DrawSpawnPositionHandle(
            current,
            new Color(0.25f, 0.55f, 1f),
            $"P{playerIndex + 1}",
            PlayerSpawnHandleSize,
            viewer,
            area);

        if (Approximately(next, current))
            return;

        next = ClampToBattleArea(next, area);
        spawn.SetPlayerSpawnPos(playerIndex, next);
    }

    static void DrawDropItemCollectLineHandle(
        BattleAreaConfigViewer viewer,
        BattleAreaData area,
        ref DropItemCollectData collect)
    {
        float currentY = collect.GetCollectLineY(in area);
        Vector3 center = new Vector3(area.Center.x, currentY, 0f);
        float size = HandleUtility.GetHandleSize(center) * CollectLineHandleSize;

        Handles.color = Color.cyan;
        Handles.DrawLine(
            new Vector3(area.Left, currentY, 0f),
            new Vector3(area.Right, currentY, 0f));

        Vector3 next = Handles.Slider(center, Vector3.up, size, Handles.ConeHandleCap, 0f);
        Handles.Label(center + Vector3.up * size * 1.8f, "吸收线", EditorStyles.miniBoldLabel);

        Vector2 snapped = SnapIfNeeded(new Vector2(area.Center.x, next.y), viewer, area);
        float nextY = Mathf.Clamp(snapped.y, area.Bottom, area.Top);
        if (Mathf.Approximately(nextY, currentY))
            return;

        collect.collectLineY = nextY;
    }

    static Vector2 DrawSpawnPositionHandle(
        Vector2 position,
        Color color,
        string label,
        float sizeScale,
        BattleAreaConfigViewer viewer,
        BattleAreaData area)
    {
        Vector2 snapped = SnapIfNeeded(position, viewer, area);
        Vector3 pos3 = new Vector3(snapped.x, snapped.y, 0f);
        float size = HandleUtility.GetHandleSize(pos3) * sizeScale;

        Handles.color = color;
        Vector3 next = Handles.FreeMoveHandle(
            pos3,
            size,
            Vector3.zero,
            Handles.SphereHandleCap);

        Handles.Label(pos3 + Vector3.up * size * 1.5f, label, EditorStyles.miniBoldLabel);
        return SnapIfNeeded(new Vector2(next.x, next.y), viewer, area);
    }

    static Vector2 SnapIfNeeded(Vector2 world, BattleAreaConfigViewer viewer, BattleAreaData area)
    {
        if (!viewer.BattleAreaToolSnapToGrid)
            return world;

        return StageTimelinePathEditSnap.SnapWorld(world, area, viewer.BattleAreaToolSnapCellSize);
    }

    static Vector2 ClampToBattleArea(Vector2 world, BattleAreaData area) =>
        new Vector2(
            Mathf.Clamp(world.x, area.Left, area.Right),
            Mathf.Clamp(world.y, area.Bottom, area.Top));

    static bool Approximately(Vector2 a, Vector2 b) =>
        Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
}
