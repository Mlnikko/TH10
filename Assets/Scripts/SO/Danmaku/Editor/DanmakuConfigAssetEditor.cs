#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="DanmakuConfig"/> 资产 Inspector：<see cref="DanmakuConfig.danmakuPrefabId"/> 使用 Manifest 下拉。
/// </summary>
[CustomEditor(typeof(DanmakuConfig))]
[CanEditMultipleObjects]
public class DanmakuConfigAssetEditor : Editor
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

            if (prop.name == nameof(DanmakuConfig.danmakuPrefabId))
            {
                ResourceIdEditorPicker.DrawPrefabIdField(
                    prop,
                    nameof(GameResourceManifest.danmakuPrefabIds),
                    "Prefabs/Danmaku");
                continue;
            }

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
