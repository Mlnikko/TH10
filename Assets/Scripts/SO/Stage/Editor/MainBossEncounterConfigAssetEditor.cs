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

            if (StageTimelinePathEditScope.ShouldHidePathRoutes(E_StageTimelinePathEditTarget.MainBoss)
                && IsPathRouteProperty(prop.name))
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
        name is nameof(MainBossEncounterConfig.spawnFrameOffset)
            or nameof(MainBossEncounterConfig.bossIntroDurationFrames)
            or nameof(MainBossEncounterConfig.entryDurationFrames)
            or nameof(MainBossEncounterConfig.entryPathRouteBakeIndex)
            or nameof(MainBossEncounterConfig.loopPathRouteBakeIndex);
}
#endif
