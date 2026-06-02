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

            if (prop.name == nameof(BossPhaseConfig.spellCardId))
            {
                ResourceIdEditorPicker.DrawDanmakuEmitterConfigIdField(prop, "dme_boss_");
                continue;
            }

            if (prop.name == nameof(BossPhaseConfig.triggerFrameOffset)
                || prop.name == nameof(BossPhaseConfig.durationFrames))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
