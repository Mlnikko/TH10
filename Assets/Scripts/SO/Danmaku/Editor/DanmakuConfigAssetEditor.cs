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

            if (prop.name == nameof(DanmakuConfig.hitEffectPrefabId))
            {
                ResourceIdEditorPicker.DrawPoolPrefabIdField(prop, E_PoolCategory.Effect);
                continue;
            }

            if (IsHomingOnlyField(prop.name))
            {
                var typeProp = serializedObject.FindProperty(nameof(DanmakuConfig.danmakuType));
                if (typeProp == null || typeProp.enumValueIndex != (int)E_DanmakuType.Homing)
                    continue;
            }

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    static bool IsHomingOnlyField(string propertyName) =>
        propertyName == nameof(DanmakuConfig.homingTargetLayers)
        || propertyName == nameof(DanmakuConfig.homingBezierDurationSeconds)
        || propertyName == nameof(DanmakuConfig.homingCurveStrength);
}
#endif
