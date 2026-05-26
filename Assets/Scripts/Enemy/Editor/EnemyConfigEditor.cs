#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(EnemyConfigViewer), true)]
public class EnemyConfigEditor : Editor
{
    const string ConfigField = "enemyConfig";
    const string PrefabIdField = "enemyPrefabId";
    const string DropIdsField = "dropOnDeathEntries";
    const string DeathFxField = "deathEffectPrefabId";

    public override void OnInspectorGUI()
    {
        var viewer = (EnemyConfigViewer)target;
        bool inPrefabStage = IsEditingInPrefabStage(viewer.gameObject);

        serializedObject.Update();

        var configRef = serializedObject.FindProperty(ConfigField);
        if (configRef != null)
            EditorGUILayout.PropertyField(configRef);

        if (inPrefabStage && viewer.EnemyConfig != null)
            DrawEnemyPrefabIdFromConfig(viewer);
        else
        {
            var prefabId = serializedObject.FindProperty(PrefabIdField);
            if (prefabId != null)
                ResourceIdEditorPicker.DrawEnemyPrefabIdField(prefabId);
        }

        DrawPropertiesExcluding(serializedObject, "m_Script", ConfigField, PrefabIdField, DropIdsField, DeathFxField);

        if (inPrefabStage && viewer.EnemyConfig != null)
            DrawDropAndDeathFromConfig(viewer);
        else
            DrawDropAndDeathFromViewer();

        serializedObject.ApplyModifiedProperties();

        ConfigViewerEditorUI.DrawSeparator();

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.EnemyConfig, "EnemyConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint(
            inPrefabStage
                ? "正在预制体编辑模式：掉落/死亡特效直接读写 EnemyConfig 资产。"
                : "双击进入预制体编辑后，将自动从 EnemyConfig 同步并可编辑掉落/死亡特效。");

        ConfigViewerEditorUI.DrawSaveButton(
            viewer.EnemyConfig,
            viewer.SaveEnemyConfig,
            "EnemyConfig");
    }

    static bool IsEditingInPrefabStage(GameObject go)
    {
        if (go == null)
            return false;

        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        return stage != null && stage.IsPartOfPrefabContents(go);
    }

    void DrawEnemyPrefabIdFromConfig(EnemyConfigViewer viewer)
    {
        var configSo = new SerializedObject(viewer.EnemyConfig);
        configSo.Update();

        var prefabId = configSo.FindProperty(PrefabIdField);
        if (prefabId != null)
            ResourceIdEditorPicker.DrawEnemyPrefabIdField(prefabId);

        if (configSo.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(viewer.EnemyConfig);
            viewer.LoadFromConfig();
            serializedObject.Update();
        }
    }

    void DrawDropAndDeathFromConfig(EnemyConfigViewer viewer)
    {
        var configSo = new SerializedObject(viewer.EnemyConfig);
        configSo.Update();

        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField("死亡掉落", EditorStyles.boldLabel);

        var dropIds = configSo.FindProperty(DropIdsField);
        if (dropIds != null)
            ResourceIdEditorPicker.DrawDeathDropEntryArray(dropIds, drawSectionHeader: false);

        var deathFx = configSo.FindProperty(DeathFxField);
        if (deathFx != null)
            ResourceIdEditorPicker.DrawPoolPrefabIdField(deathFx, E_PoolCategory.Effect);

        if (configSo.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(viewer.EnemyConfig);
            viewer.LoadFromConfig();
            serializedObject.Update();
        }

        EditorGUILayout.Space(2);
    }

    void DrawDropAndDeathFromViewer()
    {
        var dropIds = serializedObject.FindProperty(DropIdsField);
        if (dropIds != null)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("死亡掉落（预制体缓存）", EditorStyles.boldLabel);
            ResourceIdEditorPicker.DrawDeathDropEntryArray(dropIds, drawSectionHeader: false);
        }

        var deathFx = serializedObject.FindProperty(DeathFxField);
        if (deathFx != null)
            ResourceIdEditorPicker.DrawPoolPrefabIdField(deathFx, E_PoolCategory.Effect);

        EditorGUILayout.Space(2);
    }
}
#endif
