#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuEmitterConfig))]
[CanEditMultipleObjects]
public class DanmakuEmitterConfigAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "emitterPrefabId 为池 archetype（见 DanmakuEmitterPrefabArchetypes）；"
            + "displaySprite 在出池/武器布局时由 DanmakuEmitterPresentation 应用。",
            MessageType.None);

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script")
            {
                EditorGUILayout.PropertyField(prop);
                continue;
            }

            if (prop.name == nameof(DanmakuEmitterConfig.emitterPrefabId))
            {
                ResourceIdEditorPicker.DrawPrefabIdField(
                    prop,
                    nameof(GameResourceManifest.danmakuEmitterPrefabIds),
                    "Prefabs/DanmakuEmitter");
                continue;
            }

            if (prop.name == nameof(DanmakuEmitterConfig.danmakuConfigIds))
            {
                ResourceIdEditorPicker.DrawDanmakuConfigIdArray(prop);
                continue;
            }

            if (prop.name == nameof(DanmakuEmitterConfig.emitterCamp))
            {
                EditorGUILayout.PropertyField(prop, true);
                DanmakuEmitterModeInspectorUI.DrawAimAtPlayerIfEnemy(serializedObject);
                continue;
            }

            if (prop.name == DanmakuEmitterModeInspectorUI.ConfigAimAtPlayerField)
                continue;

            if (prop.name == DanmakuEmitterModeInspectorUI.ConfigEmitModeField)
            {
                EditorGUILayout.PropertyField(prop);
                var mode = DanmakuEmitterModeInspectorUI.ReadEmitMode(
                    serializedObject,
                    DanmakuEmitterModeInspectorUI.ConfigEmitModeField);
                DanmakuEmitterModeInspectorUI.DrawEmitModeHint(mode);
                var modeConfigName = DanmakuEmitterModeInspectorUI.GetModeConfigPropertyName(mode);
                if (!string.IsNullOrEmpty(modeConfigName))
                {
                    var modeConfigProp = serializedObject.FindProperty(modeConfigName);
                    if (modeConfigProp != null)
                        EditorGUILayout.PropertyField(modeConfigProp, true);
                }

                continue;
            }

            if (DanmakuEmitterModeInspectorUI.ShouldSkipProperty(
                    serializedObject,
                    prop.name,
                    DanmakuEmitterModeInspectorUI.ConfigEmitModeField))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        if (target is DanmakuEmitterConfig singleConfig)
            DanmakuEmitterModeInspectorUI.DrawEmitSalvoSummary(singleConfig);

        if (serializedObject.ApplyModifiedProperties())
        {
            foreach (Object obj in targets)
            {
                if (obj is DanmakuEmitterConfig cfg)
                    ConfigViewerPrefabSync.ApplyDanmakuEmitterDisplaySprite(cfg);
            }
        }
    }
}
#endif
