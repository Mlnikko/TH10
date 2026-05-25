#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuConfigViewer), true)]
public class DanmakuConfigViewerEditor : GameConfigViewerEditor<DanmakuConfigViewer>
{
    const string DanmakuConfigField = "danmakuConfig";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var configRef = serializedObject.FindProperty(DanmakuConfigField);
        EditorGUILayout.PropertyField(configRef, new GUIContent("配置文件"));

        DrawDanmakuPrefabIdField();

        DrawPropertiesExcluding(serializedObject, "m_Script", DanmakuConfigField);

        serializedObject.ApplyModifiedProperties();

        ConfigViewerEditorUI.DrawSeparator();
        DrawViewerTools();
    }

    void DrawDanmakuPrefabIdField()
    {
        if (Viewer.danmakuConfig == null)
        {
            EditorGUILayout.HelpBox("指定 DanmakuConfig 后可配置弹幕预制体 Id。", MessageType.None);
            return;
        }

        var cfgSo = new SerializedObject(Viewer.danmakuConfig);
        cfgSo.Update();

        EditorGUILayout.LabelField("资源引用", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var prefabId = cfgSo.FindProperty(nameof(DanmakuConfig.danmakuPrefabId));
        ResourceIdEditorPicker.DrawPrefabIdField(
            prefabId,
            nameof(GameResourceManifest.danmakuPrefabIds),
            "Prefabs/Danmaku");

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);

        cfgSo.ApplyModifiedProperties();
    }

    protected override void DrawViewerTools()
    {
        if (DrawMissingConfig(Viewer.danmakuConfig, "DanmakuConfig"))
            return;

        DrawSyncHint("双击进入弹幕预制体后自动同步；预制体 Id 写入 DanmakuConfig 资产。");

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
