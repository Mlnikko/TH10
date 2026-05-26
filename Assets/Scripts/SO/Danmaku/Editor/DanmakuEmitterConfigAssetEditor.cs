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

            if (ShouldSkipModeProperty(serializedObject, prop.name))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        if (serializedObject.ApplyModifiedProperties())
        {
            foreach (Object obj in targets)
            {
                if (obj is DanmakuEmitterConfig cfg)
                    ConfigViewerPrefabSync.ApplyDanmakuEmitterDisplaySprite(cfg);
            }
        }
    }

    static bool ShouldSkipModeProperty(SerializedObject so, string propertyName)
    {
        var modeProp = so?.FindProperty(nameof(DanmakuEmitterConfig.emitMode));
        if (modeProp == null)
            return false;

        var mode = (EmitMode)modeProp.enumValueIndex;
        return propertyName switch
        {
            nameof(DanmakuEmitterConfig.lineModeConfig) => mode != EmitMode.Line,
            nameof(DanmakuEmitterConfig.arcModeConfig) => mode != EmitMode.Arc,
            nameof(DanmakuEmitterConfig.waveModeConfig) => mode != EmitMode.Wave,
            nameof(DanmakuEmitterConfig.grainModeConfig) => mode != EmitMode.Grain,
            _ => false,
        };
    }
}
#endif
