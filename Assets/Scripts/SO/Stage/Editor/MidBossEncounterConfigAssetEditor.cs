#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MidBossEncounterConfig))]
[CanEditMultipleObjects]
public class MidBossEncounterConfigAssetEditor : Editor
{
    static readonly string[] PathRouteNames =
    {
        nameof(MidBossEncounterConfig.entryPathRoute),
        nameof(MidBossEncounterConfig.loopPathRoute),
        nameof(MidBossEncounterConfig.exitPathRoute),
    };

    static readonly string[] AnimatorStateNames =
    {
        nameof(MidBossEncounterConfig.animatorStateEntry),
        nameof(MidBossEncounterConfig.animatorStateLoop),
        nameof(MidBossEncounterConfig.animatorStateExit),
        nameof(MidBossEncounterConfig.animatorStateIdle),
        nameof(MidBossEncounterConfig.animatorStateMove),
    };

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

            if (StageTimelinePathEditScope.IsActive
                && StageTimelinePathEditScope.Target == E_StageTimelinePathEditTarget.MidBoss
                && IsPathRouteProperty(prop.name))
                continue;

            if (prop.name == nameof(MidBossEncounterConfig.enemyConfigId))
            {
                ResourceIdEditorPicker.DrawEnemyConfigIdField(prop);
                continue;
            }

            if (prop.name == nameof(MidBossEncounterConfig.animatorStateEntry))
            {
                DrawAnimatorSection();
                continue;
            }

            if (IsAnimatorStateProperty(prop.name))
                continue;

            if (prop.name == nameof(MidBossEncounterConfig.dropOnDeathEntries))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("击杀掉落", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    serializedObject.FindProperty(nameof(MidBossEncounterConfig.dropOverrideMode)),
                    new GUIContent("覆盖策略"));
                ResourceIdEditorPicker.DrawDeathDropEntryArray(prop, drawSectionHeader: false);
                EditorGUILayout.Space(2);
                continue;
            }

            if (prop.name == nameof(MidBossEncounterConfig.dropOverrideMode)
                || prop.name == nameof(MidBossEncounterConfig.dropOnDeathBaked)
                || prop.name == "dropOnDeathConfigIds"
                || IsBakeField(prop.name))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawAnimatorSection()
    {
        BossAnimatorStatePicker.DrawAnimatorStateSection(
            serializedObject,
            serializedObject.FindProperty(nameof(MidBossEncounterConfig.enemyConfigId)),
            "Animator 状态",
            new[]
            {
                (serializedObject.FindProperty(nameof(MidBossEncounterConfig.animatorStateEntry)), "入场 Entry"),
                (serializedObject.FindProperty(nameof(MidBossEncounterConfig.animatorStateLoop)), "场内 Loop"),
                (serializedObject.FindProperty(nameof(MidBossEncounterConfig.animatorStateExit)), "退场 Exit"),
                (serializedObject.FindProperty(nameof(MidBossEncounterConfig.animatorStateIdle)), "回退 Idle"),
                (serializedObject.FindProperty(nameof(MidBossEncounterConfig.animatorStateMove)), "移动 Move（未接入）"),
            });
    }

    static bool IsPathRouteProperty(string name)
    {
        for (int i = 0; i < PathRouteNames.Length; i++)
        {
            if (PathRouteNames[i] == name)
                return true;
        }

        return false;
    }

    static bool IsAnimatorStateProperty(string name)
    {
        for (int i = 0; i < AnimatorStateNames.Length; i++)
        {
            if (AnimatorStateNames[i] == name)
                return true;
        }

        return false;
    }

    static bool IsBakeField(string name) =>
        name is nameof(MidBossEncounterConfig.spawnFrameOffset)
            or nameof(MidBossEncounterConfig.onFieldDurationFrames)
            or nameof(MidBossEncounterConfig.entryDurationFrames)
            or nameof(MidBossEncounterConfig.exitDurationFrames)
            or nameof(MidBossEncounterConfig.entryPathRouteBakeIndex)
            or nameof(MidBossEncounterConfig.loopPathRouteBakeIndex)
            or nameof(MidBossEncounterConfig.exitPathRouteBakeIndex)
            or nameof(MidBossEncounterConfig.emitterConfigIndexOverride)
            or nameof(MidBossEncounterConfig.dropOnDeathBaked);
}
#endif
