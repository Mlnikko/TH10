#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponConfigViewer))]
public class WeaponConfigViewerEditor : GameConfigViewerEditor<WeaponConfigViewer>
{
    const string ConfigField = "weaponConfig";
    const string PrimaryEmittersField = "primaryEmitters";
    const string PowerPrimarySlowField = "powerPrimarySlowLayouts";
    const string PowerSecondaryField = "powerSecondaryLayouts";
    const string SlowModeLayoutField = "slowModeLayout";
    const string PreviewLayoutModeField = "previewLayoutMode";
    const string PreviewPowerOrbsField = "previewPowerOrbs";
    const string PreviewDurationField = "previewDuration";
    const string PreviewBulletLifetimeField = "previewBulletLifetime";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var previousConfig = Viewer.WeaponConfig;

        var configRef = serializedObject.FindProperty(ConfigField);
        bool configRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(
            configRef,
            new GUIContent("配置文件"));

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            ConfigField,
            PrimaryEmittersField,
            PowerPrimarySlowField,
            PowerSecondaryField,
            SlowModeLayoutField,
            PreviewLayoutModeField,
            PreviewPowerOrbsField,
            PreviewDurationField,
            PreviewBulletLifetimeField);

        var primary = serializedObject.FindProperty(PrimaryEmittersField);
        var powerSlow = serializedObject.FindProperty(PowerPrimarySlowField);
        var powerSecondary = serializedObject.FindProperty(PowerSecondaryField);
        var slowLayout = serializedObject.FindProperty(SlowModeLayoutField);

        WeaponConfigAssetEditor.DrawPrimaryEmitters(primary, powerSlow);
        if (powerSlow != null)
            ResourceIdEditorPicker.DrawWeaponPowerPrimarySlowLayouts(powerSlow);
        if (powerSecondary != null)
            ResourceIdEditorPicker.DrawWeaponPowerSecondaryLayouts(powerSecondary);

        if (slowLayout != null)
            EditorGUILayout.PropertyField(slowLayout, new GUIContent("低速收束布局"), true);

        ConfigViewerEditorUI.DrawSeparator();
        EditorGUILayout.LabelField("布局 / 发射预览", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty(PreviewLayoutModeField));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(PreviewPowerOrbsField));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(PreviewDurationField));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(PreviewBulletLifetimeField));

        serializedObject.ApplyModifiedProperties();
        ApplyConfigReferenceSync(previousConfig, Viewer.WeaponConfig, configRefChanged);

        EditorGUI.BeginChangeCheck();
        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
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

        DrawSyncHint("切换配置文件或双击进入预制体编辑后，会自动从 WeaponConfig 同步参数与布局预览。");

        EditorGUILayout.LabelField("发射预览", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "布局预览与发射预览共用「预览 Power」。低速预览会按 Power 档位显示主炮与副炮。",
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
