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

            if (StageTimelinePathEditScope.ShouldHidePathRoutes(E_StageTimelinePathEditTarget.MidBoss)
                && IsPathRouteProperty(prop.name))
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
                EditorGUILayout.LabelField("击杀掉落", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(nameof(MidBossEncounterConfig.dropOverrideMode)),
                    new GUIContent("覆盖策略"));
                ResourceIdEditorPicker.DrawDeathDropEntryArray(prop, drawSectionHeader: false);
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

        serializedObject.ApplyModifiedProperties();
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
