using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleAreaConfigViewer))]
public class BattleAreaConfigEditor : Editor
{
    const string ConfigField = "battleAreaConfig";

    public override void OnInspectorGUI()
    {
        var viewer = (BattleAreaConfigViewer)target;

        serializedObject.Update();

        var previousConfig = viewer.battleAreaConfig;

        var configRef = serializedObject.FindProperty(ConfigField);
        bool configRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(configRef);

        DrawPropertiesExcluding(serializedObject, "m_Script", ConfigField);

        serializedObject.ApplyModifiedProperties();

        ConfigViewerEditorUI.SyncViewerOnConfigReferenceChanged(
            viewer,
            previousConfig,
            viewer.battleAreaConfig,
            serializedObject,
            configRefChanged);

        ConfigViewerEditorUI.DrawSeparator();

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.battleAreaConfig, "BattleAreaConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint(
            "切换配置文件或双击进入预制体编辑后，会自动从 BattleAreaConfig 同步战斗区/出生点等参数。");
        ConfigViewerEditorUI.DrawSaveButton(
            viewer.battleAreaConfig,
            () =>
            {
                viewer.SaveBattleAreaData();
                Logger.Info($"战斗区域配置已更新：{viewer.battleAreaConfig.name}");
            },
            "BattleAreaConfig");
    }
}
