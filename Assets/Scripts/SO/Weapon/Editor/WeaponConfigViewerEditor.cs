#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponConfigViewer))]
public class WeaponConfigViewerEditor : GameConfigViewerEditor<WeaponConfigViewer>
{
    protected override void DrawViewerTools()
    {
        if (DrawMissingConfig(Viewer.WeaponConfig, "WeaponConfig"))
            return;

        DrawSyncHint("将 Viewer 放在角色发射原点；双击进入预制体后自动同步。");

        EditorGUILayout.LabelField("发射器布局", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Scene Gizmo：青色=通常，黄色=低速收束。「布局预览模式」选 Both 可同时对照。\n" +
            "「预览 Power」决定副炮档位。",
            MessageType.None);

        EditorGUILayout.LabelField("发射预览", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("预览·通常", GUILayout.Height(28)))
                Viewer.PreviewWeaponFire(WeaponEditorFirePreviewMode.Normal);

            if (GUILayout.Button("预览·低速收束", GUILayout.Height(28)))
                Viewer.PreviewWeaponFire(WeaponEditorFirePreviewMode.SlowConverge);
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
