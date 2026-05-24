#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponConfigViewer))]
public class WeaponConfigViewerEditor : GameConfigViewerEditor<WeaponConfigViewer>
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        base.OnInspectorGUI();
        bool inspectorChanged = EditorGUI.EndChangeCheck();

        if (inspectorChanged)
            Viewer.InvalidateLayoutPreview();

        if (ConfigViewerEditorScene.CanHostTransientPreview(Viewer.transform))
            Viewer.RefreshEmitterLayoutPreview();
        else
            Viewer.StopAllEditorPreviews();
    }

    protected override void DrawViewerTools()
    {
        if (DrawMissingConfig(Viewer.WeaponConfig, "WeaponConfig"))
            return;

        DrawSyncHint("将 Viewer 放在角色发射原点；双击进入预制体后自动同步。");

        EditorGUILayout.LabelField("发射预览", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "布局预览与发射预览共用「预览 Power」。点击发射预览按钮会同步切换「布局预览模式」。",
            MessageType.None);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("预览·通常", GUILayout.Height(28)))
            {
                Viewer.PreviewWeaponFire(WeaponEditorFirePreviewMode.Normal);
                Repaint();
            }

            if (GUILayout.Button("预览·低速收束", GUILayout.Height(28)))
            {
                Viewer.PreviewWeaponFire(WeaponEditorFirePreviewMode.SlowConverge);
                Repaint();
            }
        }

        if (Viewer.IsPreviewingFire)
        {
            EditorGUILayout.HelpBox("正在预览发射…", MessageType.Info);
            if (GUILayout.Button("停止发射预览", GUILayout.Height(24)))
                Viewer.StopFirePreview();
        }

        ConfigViewerEditorUI.DrawSeparator();
        DrawSave(Viewer.WeaponConfig, Viewer.SaveWeaponConfig, "WeaponConfig");
    }
}
#endif
