using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuEmitterConfigViewer), true)]
public class DanmakuEmitterConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("预览发射效果", GUILayout.Height(30)))
        {
            DanmakuEmitterConfigViewer viewer = (DanmakuEmitterConfigViewer)target;
            viewer.PreviewEmitterEffect();
        }

        if (GUILayout.Button("保存当前配置", GUILayout.Height(30)))
        {
            DanmakuEmitterConfigViewer viewer = (DanmakuEmitterConfigViewer)target;

            if (EditorUtility.DisplayDialog("确认保存？", "将覆盖资产", "确定", "取消"))
            {
                viewer.SaveEmitterConfig();
                EditorUtility.SetDirty(viewer.emitterConfig);
                AssetDatabase.SaveAssets();
            }
        }

        GUILayout.EndHorizontal();
    }
}
