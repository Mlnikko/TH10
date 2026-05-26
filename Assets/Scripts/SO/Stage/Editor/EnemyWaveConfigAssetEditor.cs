#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyWaveConfig))]
[CanEditMultipleObjects]
public class EnemyWaveConfigAssetEditor : Editor
{
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

            if (prop.name == nameof(EnemyWaveConfig.spawnQueue))
            {
                var assignmentProp = serializedObject.FindProperty(nameof(EnemyWaveConfig.pathAssignment));
                if (assignmentProp != null)
                    EditorGUILayout.PropertyField(assignmentProp, new GUIContent("路径分配"));

                ResourceIdEditorPicker.DrawWaveSpawnQueueArray(
                    prop,
                    assignmentProp,
                    drawSectionHeader: true,
                    pathEditViewer: StageTimelinePathEditScope.Viewer);
                continue;
            }

            if (prop.name == nameof(EnemyWaveConfig.pathAssignment))
                continue;

            if (prop.name == nameof(EnemyWaveConfig.pathRoute)
                && StageTimelinePathEditScope.IsActive
                && StageTimelinePathEditScope.Target == E_StageTimelinePathEditTarget.MidStageWave
                && serializedObject.targetObject is EnemyWaveConfig scopedWave
                && !scopedWave.UsesPerQueueEntryPaths)
                continue;

            if (prop.propertyPath.StartsWith(nameof(EnemyWaveConfig.spawnQueue) + ".", System.StringComparison.Ordinal))
                continue;

            if (prop.name == nameof(EnemyWaveConfig.waveDropOnDeathEntries))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("击杀掉落", EditorStyles.boldLabel);
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
                || prop.name == nameof(EnemyWaveConfig.spawnIntervalFrames))
                continue;

            if (prop.propertyPath.StartsWith(nameof(EnemyWaveConfig.waveDropOnDeathEntries) + ".", System.StringComparison.Ordinal))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
