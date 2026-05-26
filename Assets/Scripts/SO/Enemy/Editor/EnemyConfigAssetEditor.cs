#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyConfig))]
[CanEditMultipleObjects]
public class EnemyConfigAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "enemyPrefabId 为池 archetype（见 EnemyPrefabArchetypes）；"
            + "displaySprite / animatorController 在出池时由 EnemyPresentation 应用。",
            MessageType.None);

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

            if (prop.name == nameof(EnemyConfig.enemyPrefabId))
            {
                ResourceIdEditorPicker.DrawPrefabIdField(
                    prop,
                    nameof(GameResourceManifest.enemyPrefabIds),
                    "Prefabs/Enemy");
                continue;
            }

            if (prop.name == nameof(EnemyConfig.emitterConfigId))
            {
                ResourceIdEditorPicker.DrawDanmakuEmitterConfigIdField(prop);
                continue;
            }

            if (prop.name == nameof(EnemyConfig.dropOnDeathEntries))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("掉落物", EditorStyles.boldLabel);
                ResourceIdEditorPicker.DrawDeathDropEntryArray(prop, drawSectionHeader: false);
                EditorGUILayout.Space(2);
                continue;
            }

            if (prop.propertyPath.StartsWith(nameof(EnemyConfig.dropOnDeathEntries) + ".", System.StringComparison.Ordinal))
                continue;

            if (prop.name == nameof(EnemyConfig.deathEffectPrefabId))
            {
                ResourceIdEditorPicker.DrawPoolPrefabIdField(prop, E_PoolCategory.Effect);
                continue;
            }

            if (prop.name == nameof(EnemyConfig.dropOnDeathBaked)
                || prop.name == "dropOnDeathConfigIds"
                || prop.name == nameof(EnemyConfig.deathEffectPrefabIndex))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
