#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BossPhaseConfig))]
[CanEditMultipleObjects]
public class BossPhaseConfigAssetEditor : Editor
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

            if (prop.name == nameof(BossPhaseConfig.spellEmitters))
            {
                DrawSpellEmitters(prop);
                continue;
            }

            if (prop.name == nameof(BossPhaseConfig.spellCardId))
            {
                ResourceIdEditorPicker.DrawDanmakuEmitterConfigIdField(prop);
                EditorGUILayout.HelpBox(
                    "spellEmitters 非空时优先使用多发射器列表；否则回退到 spellCardId（单发射器）。",
                    MessageType.None);
                continue;
            }

            if (prop.name == nameof(BossPhaseConfig.triggerFrameOffset)
                || prop.name == nameof(BossPhaseConfig.durationFrames))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    static void DrawSpellEmitters(SerializedProperty arrayProp)
    {
        EditorGUILayout.LabelField("符卡发射器列表", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "同一阶段可配置多个 DanmakuEmitterConfig；各发射器的 initialLaunchDelaySeconds 可错开首发时间。",
            MessageType.Info);

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            var element = arrayProp.GetArrayElementAtIndex(i);
            var idProp = element.FindPropertyRelative(nameof(BossPhaseSpellEmitterEntry.emitterConfigId));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"#{i + 1}", GUILayout.Width(28));
            ResourceIdEditorPicker.DrawDanmakuEmitterConfigIdField(idProp);
            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("添加发射器"))
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
    }
}
#endif
