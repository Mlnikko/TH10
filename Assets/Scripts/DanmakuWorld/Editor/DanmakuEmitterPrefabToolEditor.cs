using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuEmitterPrefabTool), true)]
public class DanmakuEmitterPrefabToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("预览发射效果", GUILayout.Height(30)))
        {
            DanmakuEmitterPrefabTool tool = (DanmakuEmitterPrefabTool)target;
            tool.PreviewEmitterEffect();
        }

        if (GUILayout.Button("保存当前配置", GUILayout.Height(30)))
        {
            DanmakuEmitterPrefabTool tool = (DanmakuEmitterPrefabTool)target;

            if (EditorUtility.DisplayDialog("确认保存？", "将覆盖资产", "确定", "取消"))
            {
                tool.SaveEmitterConfig();
                EditorUtility.SetDirty(tool.emitterConfig);
                AssetDatabase.SaveAssets();
            }
        }

        GUILayout.EndHorizontal();
    }
}
