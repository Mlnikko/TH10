#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuConfigViewer), true)]
public class DanmakuConfigViewerEditor : GameConfigViewerEditor<DanmakuConfigViewer>
{
    protected override void DrawViewerTools()
    {
        if (DrawMissingConfig(Viewer.danmakuConfig, "DanmakuConfig"))
            return;

        DrawSyncHint();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新场景预览", GUILayout.Height(28)))
                Viewer.PreviewDanmaku();

            if (GUILayout.Button("保存到 DanmakuConfig", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("确认保存？", "将覆盖 DanmakuConfig 资产", "确定", "取消"))
                {
                    Viewer.SaveDanmakuConfig();
                    EditorUtility.SetDirty(Viewer.danmakuConfig);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}
#endif
