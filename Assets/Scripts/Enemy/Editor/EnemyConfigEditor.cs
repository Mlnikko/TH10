using UnityEditor;
using UnityEngine;
using static UnityEngine.GridBrushBase;

[CustomEditor(typeof(EnemyConfigViewer), true)]
public class EnemyConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("读取并预览敌人配置", GUILayout.Height(30)))
        {
            EnemyConfigViewer viewer = (EnemyConfigViewer)target;
            if(viewer.EnemyConfig == null)
            {
                Logger.Warn("未指定 EnemyConfig！");
                return;
            }
            viewer.LoadEnemyConfig();
            Logger.Info("敌人配置已加载并预览！");
        }

        if (GUILayout.Button("应用并保存当前配置", GUILayout.Height(30)))
        {
            EnemyConfigViewer viewer = (EnemyConfigViewer)target;
            if(viewer.EnemyConfig == null)
            {
                Logger.Warn("未指定 EnemyConfig！");
                return;
            }
            if (EditorUtility.DisplayDialog(
           "确认保存？",
           "将覆盖资产",
           "确定", "取消"))
            {
                viewer.SaveEnemyConfig();
                EditorUtility.SetDirty(viewer.EnemyConfig);
                AssetDatabase.SaveAssets();
                Logger.Info("敌人配置已保存！");
            }
        }

        GUILayout.EndHorizontal();
    }
}
