#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MidBossEncounterConfig))]
[CanEditMultipleObjects]
public class MidBossEncounterConfigAssetEditor : Editor
{
    static readonly string[] PathRouteNames =
    {
        nameof(MidBossEncounterConfig.entryPathRoute),
        nameof(MidBossEncounterConfig.loopPathRoute),
        nameof(MidBossEncounterConfig.exitPathRoute),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

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

            if (IsPathRouteProperty(prop.name))
                continue;

            if (prop.name == nameof(MidBossEncounterConfig.enemyConfigId))
            {
                ResourceIdEditorPicker.DrawEnemyConfigIdField(prop, EnemyType.MidBoss);
                continue;
            }

            if (prop.name == nameof(MidBossEncounterConfig.emitterConfigIdOverride))
            {
                ResourceIdEditorPicker.DrawDanmakuEmitterConfigIdField(prop, "dme_midboss_");
                continue;
            }

            if (prop.name == nameof(MidBossEncounterConfig.dropOnDeathEntries))
            {
                EditorGUILayout.Space(2);
                var modeProp = serializedObject.FindProperty(nameof(MidBossEncounterConfig.dropOverrideMode));
                EditorGUILayout.PropertyField(modeProp, new GUIContent("覆盖策略"));
                ResourceIdEditorPicker.DrawDeathDropEntryArray(
                    prop,
                    drawSectionHeader: false,
                    drawAddButton: ShouldDrawDropAddButton(modeProp));
                EditorGUILayout.Space(2);
                continue;
            }

            if (prop.name == nameof(MidBossEncounterConfig.dropOverrideMode)
                || prop.name == nameof(MidBossEncounterConfig.dropOnDeathBaked)
                || prop.name == "dropOnDeathConfigIds"
                || IsBakeField(prop.name))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        DrawPathRoutes();

        serializedObject.ApplyModifiedProperties();
    }

    static bool ShouldDrawDropAddButton(SerializedProperty modeProp) =>
        modeProp == null
        || modeProp.propertyType != SerializedPropertyType.Enum
        || (!modeProp.hasMultipleDifferentValues
            && modeProp.enumValueIndex != (int)E_WaveDropOverrideMode.UseEnemyConfig);

    void DrawPathRoutes()
    {
        if (serializedObject.targetObject is not MidBossEncounterConfig encounter)
            return;

        bool scopedInTimeline = StageTimelinePathEditScope.IsActive
                                && StageTimelinePathEditScope.Target == E_StageTimelinePathEditTarget.MidBoss
                                && StageTimelinePathEditScope.Viewer != null;

        EditorGUILayout.Space(4f);

        if (scopedInTimeline)
        {
            int phase = StageTimelinePathEditScope.Viewer.PreviewMidBossPathPhase;
            StageTimelineBossPathEdit.EnsureMidBossRouteInitialized(encounter, phase);
            DrawPathRouteProperty(
                PathRouteNames[Mathf.Clamp(phase, 0, PathRouteNames.Length - 1)],
                $"运动路径 · {StageTimelineBossPathEdit.GetMidBossPhaseLabel(phase)}");
            return;
        }

        for (int i = 0; i < PathRouteNames.Length; i++)
        {
            StageTimelineBossPathEdit.EnsureMidBossRouteInitialized(encounter, i);
            DrawPathRouteProperty(
                PathRouteNames[i],
                $"运动路径 · {StageTimelineBossPathEdit.GetMidBossPhaseLabel(i)}");
        }
    }

    void DrawPathRouteProperty(string propertyName, string title)
    {
        var pathProp = serializedObject.FindProperty(propertyName);
        if (pathProp == null)
            return;

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(pathProp, new GUIContent(title), includeChildren: true);
        if (!EditorGUI.EndChangeCheck())
            return;

        serializedObject.ApplyModifiedProperties();
        if (serializedObject.targetObject is MidBossEncounterConfig encounter)
            EditorUtility.SetDirty(encounter);

        StageTimelinePathEditScope.Viewer?.OnEmbeddedConfigChanged();
    }

    static bool IsPathRouteProperty(string name)
    {
        for (int i = 0; i < PathRouteNames.Length; i++)
        {
            if (PathRouteNames[i] == name)
                return true;
        }

        return false;
    }

    static bool IsBakeField(string name) =>
        name is nameof(MidBossEncounterConfig.spawnFrameOffset)
            or nameof(MidBossEncounterConfig.onFieldDurationFrames)
            or nameof(MidBossEncounterConfig.entryDurationFrames)
            or nameof(MidBossEncounterConfig.exitDurationFrames)
            or nameof(MidBossEncounterConfig.entryPathRouteBakeIndex)
            or nameof(MidBossEncounterConfig.loopPathRouteBakeIndex)
            or nameof(MidBossEncounterConfig.exitPathRouteBakeIndex)
            or nameof(MidBossEncounterConfig.emitterConfigIndexOverride)
            or nameof(MidBossEncounterConfig.dropOnDeathBaked);
}
#endif
