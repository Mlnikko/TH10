#if UNITY_EDITOR

using UnityEditor;

using UnityEngine;



/// <summary>

/// Scene 路径点拖拽：道中波次、中场 Boss、关底 Boss（复用多选 / 网格吸附）。

/// </summary>

[InitializeOnLoad]

static class StageTimelinePathRouteSceneHandles

{

    static readonly Color SpawnColor = new(1f, 0.92f, 0.2f, 0.95f);

    static readonly Color SpawnInactiveColor = new(1f, 0.92f, 0.2f, 0.45f);

    static readonly Color PhaseOriginColor = new(0.75f, 0.85f, 1f, 0.75f);



    static StageTimelinePathRouteSceneHandles()

    {

        SceneView.duringSceneGui += OnSceneGUI;

    }



    public static void KeepViewerSelected(StageTimelineConfigViewer viewer)

    {

        if (viewer != null && Selection.activeGameObject != viewer.gameObject)

            Selection.activeGameObject = viewer.gameObject;

    }



    static void OnSceneGUI(SceneView view)

    {

        var viewer = Selection.activeGameObject != null

            ? Selection.activeGameObject.GetComponent<StageTimelineConfigViewer>()

            : null;

        if (viewer == null || !viewer.TryResolveGizmoBattleArea(out BattleAreaData area))

            return;



        switch (viewer.PathEditTarget)

        {

            case E_StageTimelinePathEditTarget.MidBoss:

                DrawMidBossSceneGui(viewer, area);

                break;

            case E_StageTimelinePathEditTarget.MainBoss:

                DrawMainBossSceneGui(viewer, area);

                break;

            default:

                DrawWaveSceneGui(viewer, area);

                break;

        }

    }



    static void DrawWaveSceneGui(StageTimelineConfigViewer viewer, BattleAreaData area)

    {

        if (!viewer.TryGetActiveWaveGizmoContext(out EnemyWaveConfig wave, out int waveIndex, out _))

            return;



        DrawWaveSpawnPointPickers(viewer, wave, waveIndex, area);



        if (!viewer.TryGetActiveWavePathEditContext(

                out wave,

                out waveIndex,

                out int pathEditEntryIndex,

                out BattleAreaData pathArea))

            return;

        area = pathArea;



        if (wave.UsesPerQueueEntryPaths)

            wave.EnsureEntryPathOverrideInitialized(pathEditEntryIndex);



        var route = wave.ResolveEditablePathRoute(pathEditEntryIndex);

        if (!EnemyWaveSpawnMath.TryResolveQueueEntrySpawn(wave, area, waveIndex, 0, pathEditEntryIndex, out Vector2 spawn))

            return;



        DrawRouteNodeHandles(

            viewer,

            wave,

            route,

            spawn,

            area,

            contextKey: pathEditEntryIndex);

    }



    static void DrawMidBossSceneGui(StageTimelineConfigViewer viewer, BattleAreaData area)

    {

        var encounter = viewer.stageTimelineConfig?.midBossEncounter;

        if (encounter == null || !encounter.enabled)

            return;



        uint fps = LogicFramePreviewClock.GetLogicFps();

        DrawBossPhaseOriginPickers(

            viewer,

            encounter,

            area,

            fps,

            StageTimelineBossPathEdit.MidBossPhaseCount,

            i => StageTimelineBossPathEdit.GetMidBossPhaseLabel(i),

            i => StageTimelineBossPathEdit.ResolveMidBossPhaseOrigin(encounter, i, area, fps),

            viewer.PreviewMidBossPathPhase,

            phase => viewer.SetMidBossPathPhase(phase));



        int phase = viewer.PreviewMidBossPathPhase;

        StageTimelineBossPathEdit.EnsureMidBossRouteInitialized(encounter, phase);

        var route = StageTimelineBossPathEdit.GetMidBossRoute(encounter, phase);

        Vector2 origin = StageTimelineBossPathEdit.ResolveMidBossPhaseOrigin(encounter, phase, area, fps);



        DrawRouteNodeHandles(
            viewer,
            encounter,
            route,
            origin,
            area,
            contextKey: MakeBossPathContextKey(E_StageTimelinePathEditTarget.MidBoss, phase));

    }



    static void DrawMainBossSceneGui(StageTimelineConfigViewer viewer, BattleAreaData area)

    {

        var encounter = viewer.stageTimelineConfig?.mainBossEncounter;

        if (encounter == null || !encounter.enabled)

            return;



        uint fps = LogicFramePreviewClock.GetLogicFps();

        DrawBossPhaseOriginPickers(

            viewer,

            encounter,

            area,

            fps,

            StageTimelineBossPathEdit.MainBossPhaseCount,

            i => StageTimelineBossPathEdit.GetMainBossPhaseLabel(i),

            i => StageTimelineBossPathEdit.ResolveMainBossPhaseOrigin(encounter, i, area, fps),

            viewer.PreviewMainBossPathPhase,

            phase => viewer.SetMainBossPathPhase(phase));



        int phase = viewer.PreviewMainBossPathPhase;

        StageTimelineBossPathEdit.EnsureMainBossRouteInitialized(encounter, phase);

        var route = StageTimelineBossPathEdit.GetMainBossRoute(encounter, phase);

        Vector2 origin = StageTimelineBossPathEdit.ResolveMainBossPhaseOrigin(encounter, phase, area, fps);



        DrawRouteNodeHandles(
            viewer,
            encounter,
            route,
            origin,
            area,
            contextKey: MakeBossPathContextKey(E_StageTimelinePathEditTarget.MainBoss, phase));

    }



    static int MakeBossPathContextKey(E_StageTimelinePathEditTarget target, int phase) =>
        (int)target * 16 + phase;



    static void DrawBossPhaseOriginPickers(

        StageTimelineConfigViewer viewer,

        UnityEngine.Object undoTarget,

        BattleAreaData area,

        uint logicFps,

        int phaseCount,

        System.Func<int, string> labelForPhase,

        System.Func<int, Vector2> originForPhase,

        int activePhase,

        System.Action<int> onSelectPhase)

    {

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;



        for (int phase = 0; phase < phaseCount; phase++)

        {

            Vector2 origin = originForPhase(phase);

            bool isActive = phase == activePhase;

            var origin3 = new Vector3(origin.x, origin.y, 0f);

            float handleSize = HandleUtility.GetHandleSize(origin3) * (isActive ? 0.14f : 0.1f);



            Handles.color = phase == 0

                ? (isActive ? SpawnColor : SpawnInactiveColor)

                : (isActive ? PhaseOriginColor : new Color(PhaseOriginColor.r, PhaseOriginColor.g, PhaseOriginColor.b, 0.4f));



            if (Handles.Button(origin3, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))

            {

                if (!isActive)

                {

                    Undo.RecordObject(viewer, "Select Boss Path Phase");

                    onSelectPhase(phase);

                }



                KeepViewerSelected(viewer);

                GUIUtility.hotControl = 0;

                Event.current.Use();

            }



            string label = isActive ? $"{labelForPhase(phase)} ▶" : labelForPhase(phase);

            Handles.Label(origin3 + Vector3.up * (handleSize * 0.75f), label);

        }

    }



    static void DrawWaveSpawnPointPickers(

        StageTimelineConfigViewer viewer,

        EnemyWaveConfig wave,

        int waveIndex,

        BattleAreaData area)

    {

        wave.EnsureSpawnQueueMigrated();

        int entryCount = wave.ResolveSpawnCount();

        if (entryCount <= 1)

            return;



        int activeEntry = Mathf.Clamp(viewer.PreviewPathEditEntryIndex, 0, entryCount - 1);

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;



        for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)

        {

            if (!EnemyWaveSpawnMath.TryResolveQueueEntrySpawn(wave, area, waveIndex, 0, entryIndex, out Vector2 spawn))

                continue;



            bool isActive = entryIndex == activeEntry;

            var spawn3 = new Vector3(spawn.x, spawn.y, 0f);

            float handleSize = HandleUtility.GetHandleSize(spawn3) * (isActive ? 0.16f : 0.12f);



            Handles.color = isActive ? SpawnColor : SpawnInactiveColor;

            if (Handles.Button(spawn3, Quaternion.identity, handleSize, handleSize, Handles.SphereHandleCap))

            {

                if (!isActive)

                {

                    Undo.RecordObject(viewer, "Select Path Edit Entry");

                    viewer.SetPreviewPathEditEntryIndex(entryIndex);

                }



                KeepViewerSelected(viewer);

                GUIUtility.hotControl = 0;

                Event.current.Use();

            }

        }

    }



    static void DrawRouteNodeHandles(

        StageTimelineConfigViewer viewer,

        UnityEngine.Object undoTarget,

        PathRouteMovementData route,

        Vector2 spawn,

        BattleAreaData area,

        int contextKey)

    {

        route?.EnsureSpawnAnchoredFormat();

        if (route == null)

            return;



        if (route.nodes == null || route.nodes.Count < 1)

        {

            route.nodes = new System.Collections.Generic.List<MovementPathNode>

            {

                new() { positionLocal = UnityEngine.Vector2.down * 16f }

            };

            route.EnsureLegsMatchNodeCount();

            EditorUtility.SetDirty(undoTarget);

        }

        else

        {

            route.EnsureLegsMatchNodeCount();

        }



        StageTimelinePathNodeMultiEdit.EnsureContext(viewer, undoTarget, contextKey);



        bool snap = viewer.PathNodeSnapToGrid;

        float cell = viewer.PathNodeSnapCellSize;

        if (viewer.DrawPathNodeSnapGrid && snap)

            StageTimelinePathEditSnap.DrawGridForBattleArea(area, cell);



        bool changed = false;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;



        StageTimelinePathNodeMultiEdit.HandleInput(viewer, spawn, area, route.nodes, snap, cell, ref changed);

        StageTimelinePathNodeMultiEdit.DrawNodes(viewer, undoTarget, spawn, area, route, snap, cell, ref changed);



        Handles.color = new Color(1f, 1f, 1f, 0.35f);

        Vector3 prev = new Vector3(spawn.x, spawn.y, 0f);

        for (int i = 0; i < route.nodes.Count; i++)

        {

            var n = route.nodes[i];

            Vector3 p = new Vector3(spawn.x + n.positionLocal.x, spawn.y + n.positionLocal.y, 0f);

            Handles.DrawDottedLine(prev, p, 4f);

            prev = p;

        }



        if (changed)

        {

            route.EnsureLegsMatchNodeCount();

            EditorUtility.SetDirty(undoTarget);

            viewer.OnEmbeddedConfigChanged();

            SceneView.RepaintAll();

        }

    }

}

#endif


