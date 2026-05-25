#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyWaveConfig))]
[CanEditMultipleObjects]
public class EnemyWaveConfigAssetEditor : Editor
{
    const string WaveDropIdsField = "waveDropOnDeathConfigIds";

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

            if (prop.name == nameof(EnemyWaveConfig.enemyConfigId))
            {
                ResourceIdEditorPicker.DrawEnemyConfigIdField(prop);
                continue;
            }

            if (prop.name == WaveDropIdsField)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("击杀掉落", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(nameof(EnemyWaveConfig.waveDropMode)),
                    new GUIContent("覆盖策略"));
                ResourceIdEditorPicker.DrawDropItemConfigIdArray(prop, drawSectionHeader: false);
                EditorGUILayout.Space(2);
                continue;
            }

            if (prop.name == nameof(EnemyWaveConfig.waveDropMode))
                continue;

            if (prop.name == nameof(EnemyWaveConfig.waveDropOnDeathCfgIndices))
                continue;

            if (prop.name == nameof(EnemyWaveConfig.startFrameOffset))
                continue;

            if (prop.name == nameof(EnemyWaveConfig.defaultDescentSpeedPerFrame))
                continue;

            if (prop.propertyPath.StartsWith(WaveDropIdsField + ".", System.StringComparison.Ordinal))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
