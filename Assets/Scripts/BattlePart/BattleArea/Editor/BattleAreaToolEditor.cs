using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleAreaTool))]
public class BattleAreaToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        BattleAreaTool tool = (BattleAreaTool)target;

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