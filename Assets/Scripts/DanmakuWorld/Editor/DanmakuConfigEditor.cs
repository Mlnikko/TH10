using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuConfigViewer), true)]
public class DanmakuConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("预览弹幕表现", GUILayout.Height(30)))
        {
            DanmakuConfigViewer viewer = (DanmakuConfigViewer)target;
            viewer.PreviewDanmaku();
        }

        if (GUILayout.Button("保存弹幕配置", GUILayout.Height(30)))
        {
            DanmakuConfigViewer viewer = (DanmakuConfigViewer)target;

            if (EditorUtility.DisplayDialog(
            "确认保存？",
            "将覆盖资产",
            "确定", "取消"))
            {
                viewer.SaveDanmakuConfig();
                EditorUtility.SetDirty(viewer.danmakuConfig);
                AssetDatabase.SaveAssets();
            }
        }

        GUILayout.EndHorizontal();
    }
}
