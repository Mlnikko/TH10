#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyWaveConfig))]
[CanEditMultipleObjects]
public class EnemyWaveConfigAssetEditor : Editor
{
    bool _deferredSpawnQueueDrawn;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        _deferredSpawnQueueDrawn = false;

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script")
            {
                EditorGUILayout.PropertyField(prop);
                continue;
            }

            if (prop.name == nameof(EnemyWaveConfig.waveTitle))
            {
                DrawWaveTitleField(serializedObject, prop);
                continue;
            }

            if (prop.name == nameof(EnemyWaveConfig.spawnQueue)
                || prop.name == nameof(EnemyWaveConfig.pathAssignment))
                continue;

            if (prop.name == nameof(EnemyWaveConfig.pathRoute))
                continue;

            if (prop.propertyPath.StartsWith(nameof(EnemyWaveConfig.spawnQueue) + ".", System.StringComparison.Ordinal))
                continue;

            if (prop.name == nameof(EnemyWaveConfig.waveDropOnDeathEntries))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(nameof(EnemyWaveConfig.waveDropMode)),
                    new GUIContent("覆盖策略"));
                ResourceIdEditorPicker.DrawDeathDropEntryArray(prop, drawSectionHeader: false);
                EditorGUILayout.Space(2);
                continue;
            }

            if (prop.name == nameof(EnemyWaveConfig.waveDropMode))
                continue;

            if (prop.name == nameof(EnemyWaveConfig.waveDropOnDeathBaked)
                || prop.name == "waveDropOnDeathConfigIds"
                || prop.name == "enemyConfigId"
                || prop.name == "count"
                || prop.name == nameof(EnemyWaveConfig.startFrameOffset)
                || prop.name == nameof(EnemyWaveConfig.defaultDescentSpeedPerFrame)
                || prop.name == nameof(EnemyWaveConfig.pathRouteBakeIndex)
                || prop.name == nameof(EnemyWaveConfig.spawnQueuePathBakeIndices)
                || prop.name == nameof(EnemyWaveConfig.spawnQueueEmitterBakeIndices)
                || prop.name == nameof(EnemyWaveConfig.spawnIntervalFrames))
                continue;

            if (prop.propertyPath.StartsWith(nameof(EnemyWaveConfig.waveDropOnDeathEntries) + ".", System.StringComparison.Ordinal))
                continue;

            EditorGUILayout.PropertyField(prop, true);

            if (!_deferredSpawnQueueDrawn && prop.name == nameof(EnemyWaveConfig.spawnOffset))
                DrawDeferredSpawnQueueAndPathAssignment();
        }

        if (!_deferredSpawnQueueDrawn)
            DrawDeferredSpawnQueueAndPathAssignment();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>可编辑波次名称 +「自动填充」按钮（内嵌 Inspector 复用）。</summary>
    public static void DrawWaveTitleField(SerializedObject serializedObject, SerializedProperty waveTitleProp)
    {
        if (waveTitleProp == null)
            return;

        var wave = serializedObject.targetObject as EnemyWaveConfig;

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(
            waveTitleProp,
            new GUIContent("波次名称", "留空时时间轴显示按配置自动生成的摘要"));
        if (GUILayout.Button(new GUIContent("自动填充", "按当前队列/阵型/路径生成标准标题"), GUILayout.Width(64f)))
        {
            if (wave != null && !serializedObject.isEditingMultipleObjects)
                waveTitleProp.stringValue = wave.BuildAutoWaveTitle();
        }

        EditorGUILayout.EndHorizontal();
        if (EditorGUI.EndChangeCheck())
            serializedObject.ApplyModifiedProperties();
    }

    void DrawDeferredSpawnQueueAndPathAssignment()
    {
        if (_deferredSpawnQueueDrawn)
            return;

        _deferredSpawnQueueDrawn = true;

        var queueProp = serializedObject.FindProperty(nameof(EnemyWaveConfig.spawnQueue));
        if (queueProp != null)
        {
            ResourceIdEditorPicker.DrawWaveSpawnQueueArray(
                queueProp,
                drawSectionHeader: true,
                pathEditViewer: StageTimelinePathEditScope.Viewer);
        }

        var assignmentProp = serializedObject.FindProperty(nameof(EnemyWaveConfig.pathAssignment));
        if (assignmentProp != null)
            EditorGUILayout.PropertyField(assignmentProp, new GUIContent("路径分配"));

        if (serializedObject.targetObject is EnemyWaveConfig wave)
            DrawPathEditEntryIndexSlider(StageTimelinePathEditScope.Viewer, wave);

        DrawSharedPathRouteIfNeeded();
        DrawPerEntryPathRouteIfNeeded();
    }

    /// <summary>多生成点时切换 Scene 路径预览锚点（Shared=锚定条目，PerQueueEntry=当前队列条目）。</summary>
    public static void DrawPathEditEntryIndexSlider(StageTimelineConfigViewer viewer, EnemyWaveConfig wave)
    {
        if (viewer == null || wave == null)
            return;

        wave.EnsureSpawnQueueMigrated();
        int count = wave.ResolveSpawnCount();
        if (count <= 1)
            return;

        string label = wave.UsesPerQueueEntryPaths
            ? "当前路径条目"
            : "路径锚定条目";
        string tooltip = wave.UsesPerQueueEntryPaths
            ? "Scene 路径 Gizmo 对应该队列条目；点击 Scene 生成点或队列「路径」切换。"
            : "全队共用 pathRoute；Scene 按该条目的生成点展示与编辑路径。";

        EditorGUI.BeginChangeCheck();
        int next = EditorGUILayout.IntSlider(
            new GUIContent(label, tooltip),
            Mathf.Clamp(viewer.PreviewPathEditEntryIndex, 0, count - 1),
            0,
            count - 1);
        if (EditorGUI.EndChangeCheck())
            viewer.SetPreviewPathEditEntryIndex(next);
    }

    void DrawSharedPathRouteIfNeeded()
    {
        if (serializedObject.targetObject is not EnemyWaveConfig wave || wave.UsesPerQueueEntryPaths)
            return;

        var pathProp = serializedObject.FindProperty(nameof(EnemyWaveConfig.pathRoute));
        if (pathProp == null)
            return;

        wave.EnsureSpawnQueueMigrated();
        int entryIndex = StageTimelinePathEditScope.Viewer != null
            ? StageTimelinePathEditScope.Viewer.PreviewPathEditEntryIndex
            : 0;
        entryIndex = wave.ResolvePathDisplayEntryIndex(entryIndex);

        EditorGUILayout.Space(4f);
        string title = wave.ResolveSpawnCount() > 1
            ? $"运动路径 · 全队共享（锚定 #{entryIndex + 1}）"
            : "运动路径 · 全队共享";

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(pathProp, new GUIContent(title), includeChildren: true);
        if (!EditorGUI.EndChangeCheck())
            return;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(wave);
        StageTimelinePathEditScope.Viewer?.OnEmbeddedConfigChanged();
    }

    void DrawPerEntryPathRouteIfNeeded()
    {
        if (serializedObject.targetObject is not EnemyWaveConfig wave || !wave.UsesPerQueueEntryPaths)
            return;

        var queueProp = serializedObject.FindProperty(nameof(EnemyWaveConfig.spawnQueue));
        if (queueProp == null || !queueProp.isArray || queueProp.arraySize <= 0)
            return;

        wave.EnsureSpawnQueueMigrated();
        int entryIndex = StageTimelinePathEditScope.Viewer != null
            ? wave.ResolvePathDisplayEntryIndex(StageTimelinePathEditScope.Viewer.PreviewPathEditEntryIndex)
            : 0;
        entryIndex = Mathf.Clamp(entryIndex, 0, queueProp.arraySize - 1);

        wave.EnsureEntryPathOverrideInitialized(entryIndex);
        serializedObject.Update();

        var pathProp = queueProp
            .GetArrayElementAtIndex(entryIndex)
            .FindPropertyRelative(nameof(WaveSpawnQueueEntry.pathRouteOverride));
        if (pathProp == null)
            return;

        EditorGUILayout.Space(4f);
        string title = queueProp.arraySize > 1
            ? $"运动路径 · 条目 #{entryIndex + 1}"
            : "运动路径 · 单独分配";

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(pathProp, new GUIContent(title), includeChildren: true);
        if (!EditorGUI.EndChangeCheck())
            return;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(wave);
        StageTimelinePathEditScope.Viewer?.OnEmbeddedConfigChanged();
    }
}
#endif
