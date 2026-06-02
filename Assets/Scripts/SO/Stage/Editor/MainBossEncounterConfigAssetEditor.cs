#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainBossEncounterConfig))]
[CanEditMultipleObjects]
public class MainBossEncounterConfigAssetEditor : Editor
{
    static readonly string[] PathRouteNames =
    {
        nameof(MainBossEncounterConfig.entryPathRoute),
        nameof(MainBossEncounterConfig.loopPathRoute),
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

            if (prop.name == nameof(MainBossEncounterConfig.enemyConfigId))
            {
                ResourceIdEditorPicker.DrawEnemyConfigIdField(prop, EnemyType.Boss);
                continue;
            }

            if (IsBakeField(prop.name))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        DrawPathRoutes();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawPathRoutes()
    {
        if (serializedObject.targetObject is not MainBossEncounterConfig encounter)
            return;

        bool scopedInTimeline = StageTimelinePathEditScope.IsActive
                                && StageTimelinePathEditScope.Target == E_StageTimelinePathEditTarget.MainBoss
                                && StageTimelinePathEditScope.Viewer != null;

        EditorGUILayout.Space(4f);

        if (scopedInTimeline)
        {
            int phase = StageTimelinePathEditScope.Viewer.PreviewMainBossPathPhase;
            StageTimelineBossPathEdit.EnsureMainBossRouteInitialized(encounter, phase);
            DrawPathRouteProperty(
                PathRouteNames[Mathf.Clamp(phase, 0, PathRouteNames.Length - 1)],
                $"运动路径 · {StageTimelineBossPathEdit.GetMainBossPhaseLabel(phase)}");
            return;
        }

        for (int i = 0; i < PathRouteNames.Length; i++)
        {
            StageTimelineBossPathEdit.EnsureMainBossRouteInitialized(encounter, i);
            DrawPathRouteProperty(
                PathRouteNames[i],
                $"运动路径 · {StageTimelineBossPathEdit.GetMainBossPhaseLabel(i)}");
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
        if (serializedObject.targetObject is MainBossEncounterConfig encounter)
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
        name is nameof(MainBossEncounterConfig.spawnFrameOffset)
            or nameof(MainBossEncounterConfig.bossIntroDurationFrames)
            or nameof(MainBossEncounterConfig.entryDurationFrames)
            or nameof(MainBossEncounterConfig.entryPathRouteBakeIndex)
            or nameof(MainBossEncounterConfig.loopPathRouteBakeIndex);
}
#endif
