#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuConfigViewer), true)]
public class DanmakuConfigViewerEditor : GameConfigViewerEditor<DanmakuConfigViewer>
{
    const string ConfigField = "danmakuConfig";
    const string PrefabIdField = "danmakuPrefabId";
    const string HitEffectField = "hitEffectPrefabId";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var previousConfig = Viewer.danmakuConfig;

        var configRef = serializedObject.FindProperty(ConfigField);
        bool configRefChanged = ConfigViewerEditorUI.DrawConfigReferenceProperty(configRef);

        var prefabId = serializedObject.FindProperty(PrefabIdField);
        if (prefabId != null)
            ResourceIdEditorPicker.DrawDanmakuPrefabIdField(prefabId);

        var hitEffect = serializedObject.FindProperty(HitEffectField);
        if (hitEffect != null)
        {
            var rect = EditorGUILayout.GetControlRect(true);
            ResourceIdEditorPicker.DrawPoolPrefabIdAtRect(
                rect,
                hitEffect,
                new GUIContent("命中特效预制体Id"),
                E_PoolCategory.Effect);
        }

        DrawPropertiesExcluding(serializedObject, "m_Script", ConfigField, PrefabIdField, HitEffectField);

        serializedObject.ApplyModifiedProperties();
        ApplyConfigReferenceSync(previousConfig, Viewer.danmakuConfig, configRefChanged);

        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    protected override void DrawViewerTools()
    {
        if (DrawMissingConfig(Viewer.danmakuConfig, "DanmakuConfig"))
            return;

        DrawSyncHint("切换配置文件或双击进入预制体编辑后，会自动从 DanmakuConfig 同步参数与 Sprite 预览。");

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
