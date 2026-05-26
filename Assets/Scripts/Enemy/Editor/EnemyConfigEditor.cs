#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyConfigViewer), true)]
public class EnemyConfigEditor : Editor
{
    const string ConfigField = "enemyConfig";

    public override void OnInspectorGUI()
    {
        var viewer = (EnemyConfigViewer)target;

        serializedObject.Update();

        var previousConfig = viewer.EnemyConfig;
        var configRef = serializedObject.FindProperty(ConfigField);
        bool configRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(configRef);

        DrawPropertiesExcluding(serializedObject, "m_Script", ConfigField);

        if (viewer.EnemyConfig != null)
            DrawEnemyConfigOnAsset(viewer);

        serializedObject.ApplyModifiedProperties();

        ConfigViewerEditorUI.SyncViewerOnConfigReferenceChanged(
            viewer,
            previousConfig,
            viewer.EnemyConfig,
            serializedObject,
            configRefChanged);

        serializedObject.Update();
        ConfigViewerEditorUI.DrawSeparator();

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.EnemyConfig, "EnemyConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint(
            "表现 / 掉落 / 死亡特效在 EnemyConfig 资产上编辑；下方字段保存到 Config 的 HP / 类型 / 碰撞。");

        ConfigViewerEditorUI.DrawSaveButton(
            viewer.EnemyConfig,
            viewer.SaveEnemyConfig,
            "EnemyConfig");
    }

    static void DrawEnemyConfigOnAsset(EnemyConfigViewer viewer)
    {
        ConfigViewerEditorUI.DrawDirectConfigEditor(
            viewer.EnemyConfig,
            viewer,
            null,
            DrawEnemyConfigFields);
    }

    static void DrawEnemyConfigFields(SerializedObject configSo)
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("表现", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            $"池预制体固定为 {EnemyPrefabArchetypes.Unit}；出池时由 EnemyPresentation 应用。",
            MessageType.None);

        DrawProperty(configSo, nameof(EnemyConfig.displaySprite));
        DrawProperty(configSo, nameof(EnemyConfig.animatorController));
        DrawProperty(configSo, nameof(EnemyConfig.emitterConfigId), ResourceIdEditorPicker.DrawDanmakuEmitterConfigIdField);

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("死亡掉落", EditorStyles.boldLabel);

        var dropIds = configSo.FindProperty(nameof(EnemyConfig.dropOnDeathEntries));
        if (dropIds != null)
            ResourceIdEditorPicker.DrawDeathDropEntryArray(dropIds, drawSectionHeader: false);

        var deathFx = configSo.FindProperty(nameof(EnemyConfig.deathEffectPrefabId));
        if (deathFx != null)
            ResourceIdEditorPicker.DrawPoolPrefabIdField(deathFx, E_PoolCategory.Effect);

        EditorGUILayout.Space(2);
    }

    static void DrawProperty(SerializedObject configSo, string propertyName, System.Action<SerializedProperty> customDrawer = null)
    {
        var prop = configSo.FindProperty(propertyName);
        if (prop == null)
            return;

        if (customDrawer != null)
            customDrawer(prop);
        else
            EditorGUILayout.PropertyField(prop);
    }
}
#endif
