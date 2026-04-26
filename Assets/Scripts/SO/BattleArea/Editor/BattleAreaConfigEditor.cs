using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleAreaConfigViewer))]
public class BattleAreaConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BattleAreaConfigViewer tool = (BattleAreaConfigViewer)target;

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("预览配置效果"))
        {
            tool.LoadBattleAreaData();
        }

        if (GUILayout.Button("保存当前配置"))
        {
            if (tool.battleAreaConfig == null)
            {
                Logger.Warn("未指定 BattleAreaConfig！");
                return;
            }

            tool.SaveBattleAreaData();
            EditorUtility.SetDirty(tool.battleAreaConfig);
            AssetDatabase.SaveAssets();

            Logger.Info($"战斗区域配置已更新：{tool.battleAreaConfig.name}");
        }

        GUILayout.EndHorizontal();
    }
}