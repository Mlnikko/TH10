#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// GameConfig 资产 Inspector 共用绘制逻辑。
/// </summary>
public static class ConfigAssetEditorUtility
{
    public static void DrawDefaultPropertiesExcept(
        SerializedObject serializedObject,
        params string[] skipPropertyNames)
    {
        serializedObject.Update();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script" || Array.IndexOf(skipPropertyNames, prop.name) >= 0)
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
