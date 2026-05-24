#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="DanmakuEmitterConfig"/> 资产 Inspector：预制体 / 弹幕 ConfigId 使用 Manifest 下拉。
/// </summary>
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

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
