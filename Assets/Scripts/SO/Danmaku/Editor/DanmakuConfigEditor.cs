using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuConfigViewer), true)]
public class DanmakuConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var viewer = (DanmakuConfigViewer)target;

        ConfigViewerEditorUI.DrawSeparator();

        if (ConfigViewerEditorUI.DrawMissingConfigWarning(viewer.danmakuConfig, "DanmakuConfig"))
            return;

        ConfigViewerEditorUI.DrawPrefabSyncHint();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("刷新场景预览", GUILayout.Height(28)))
            viewer.PreviewDanmaku();

        if (GUILayout.Button("保存到 DanmakuConfig", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("确认保存？", "将覆盖 DanmakuConfig 资产", "确定", "取消"))
            {
                viewer.SaveDanmakuConfig();
                EditorUtility.SetDirty(viewer.danmakuConfig);
                AssetDatabase.SaveAssets();
            }
        }

        GUILayout.EndHorizontal();
    }
}
