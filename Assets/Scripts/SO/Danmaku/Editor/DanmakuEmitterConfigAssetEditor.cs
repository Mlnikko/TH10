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
}
#endif
