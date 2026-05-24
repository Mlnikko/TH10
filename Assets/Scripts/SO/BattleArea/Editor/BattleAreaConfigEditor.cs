using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleAreaConfigViewer))]
public class BattleAreaConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var viewer = (BattleAreaConfigViewer)target;

        ConfigViewerEditorUI.DrawSeparator();

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.battleAreaConfig, "BattleAreaConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint(
            "双击进入预制体编辑后自动同步；Scene 视图选中物体可查看 Gizmo。调节 GridCellSize 时可见淡绿色碰撞网格。");
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
