#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainBossEncounterConfig))]
[CanEditMultipleObjects]
public class MainBossEncounterConfigAssetEditor : Editor
{
    static readonly string[] PathRouteNames =
    {
        nameof(MainBossEncounterConfig.entryPathRoute),
        nameof(MainBossEncounterConfig.loopPathRoute),
    };

    static readonly string[] AnimatorStateNames =
    {
        nameof(MainBossEncounterConfig.animatorStateIntro),
        nameof(MainBossEncounterConfig.animatorStateFight),
        nameof(MainBossEncounterConfig.animatorStateDefeated),
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
                && StageTimelinePathEditScope.Target == E_StageTimelinePathEditTarget.MainBoss
                && IsPathRouteProperty(prop.name))
                continue;

            if (prop.name == nameof(MainBossEncounterConfig.enemyConfigId))
            {
                ResourceIdEditorPicker.DrawEnemyConfigIdField(prop);
                continue;
            }

            if (prop.name == nameof(MainBossEncounterConfig.animatorStateIntro))
            {
                DrawAnimatorSection();
                continue;
            }

            if (IsAnimatorStateProperty(prop.name))
                continue;

            if (IsBakeField(prop.name))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawAnimatorSection()
    {
        BossAnimatorStatePicker.DrawAnimatorStateSection(
            serializedObject,
            serializedObject.FindProperty(nameof(MainBossEncounterConfig.enemyConfigId)),
            "Animator 状态",
            new[]
            {
                (serializedObject.FindProperty(nameof(MainBossEncounterConfig.animatorStateIntro)), "登场 BossIntro"),
                (serializedObject.FindProperty(nameof(MainBossEncounterConfig.animatorStateFight)), "符卡战 BossFight"),
                (serializedObject.FindProperty(nameof(MainBossEncounterConfig.animatorStateDefeated)), "击破 BossDefeated"),
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
        name is nameof(MainBossEncounterConfig.spawnFrameOffset)
            or nameof(MainBossEncounterConfig.bossIntroDurationFrames)
            or nameof(MainBossEncounterConfig.entryDurationFrames)
            or nameof(MainBossEncounterConfig.entryPathRouteBakeIndex)
            or nameof(MainBossEncounterConfig.loopPathRouteBakeIndex);
}
#endif
