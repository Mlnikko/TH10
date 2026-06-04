#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 关底 Boss 符卡阶段：Inspector 选择与内嵌编辑（<see cref="StageTimelineConfigEditor"/>）。
/// </summary>
static class StageTimelineBossSpellPhaseEdit
{
    public static int GetPhaseCount(MainBossEncounterConfig encounter) =>
        encounter?.bossPhases?.Count ?? 0;

    public static string GetPhaseLabel(MainBossEncounterConfig encounter, int phaseIndex)
    {
        if (encounter?.bossPhases == null
            || phaseIndex < 0
            || phaseIndex >= encounter.bossPhases.Count)
            return $"阶段 {phaseIndex}";

        var phase = encounter.bossPhases[phaseIndex];
        if (phase == null)
            return $"阶段 {phaseIndex}（空）";

        if (!string.IsNullOrWhiteSpace(phase.phaseName))
            return phase.phaseName.Trim();

        if (phase.spellEmitters != null && phase.spellEmitters.Length > 0)
        {
            int count = 0;
            for (int i = 0; i < phase.spellEmitters.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(phase.spellEmitters[i].emitterConfigId))
                    count++;
            }

            if (count > 0)
                return count == 1
                    ? phase.spellEmitters[0].emitterConfigId.Trim()
                    : $"{count} 发射器";
        }

        if (!string.IsNullOrWhiteSpace(phase.spellCardId))
            return phase.spellCardId.Trim();

        return string.IsNullOrWhiteSpace(phase.name) ? $"阶段 {phaseIndex}" : phase.name;
    }

    public static void DrawPhaseToolbar(
        MainBossEncounterConfig encounter,
        int currentPhaseIndex,
        Action<int> onPhaseChanged)
    {
        int count = GetPhaseCount(encounter);
        if (count <= 0)
        {
            EditorGUILayout.HelpBox("尚未配置符卡阶段。可在下方添加 BossPhaseConfig。", MessageType.None);
            return;
        }

        int clamped = Mathf.Clamp(currentPhaseIndex, 0, count - 1);
        var labels = new string[count];
        for (int i = 0; i < count; i++)
            labels[i] = GetPhaseLabel(encounter, i);

        EditorGUI.BeginChangeCheck();
        int next = GUILayout.Toolbar(clamped, labels);
        if (EditorGUI.EndChangeCheck())
            onPhaseChanged(next);
    }

    public static void DrawSelectedPhaseInspector(
        StageTimelineConfigViewer viewer,
        MainBossEncounterConfig encounter,
        int phaseIndex)
    {
        if (encounter?.bossPhases == null || phaseIndex < 0 || phaseIndex >= encounter.bossPhases.Count)
            return;

        var phase = encounter.bossPhases[phaseIndex];
        if (phase == null)
        {
            EditorGUILayout.HelpBox($"符卡阶段 [{phaseIndex}] 为空。", MessageType.Warning);
            return;
        }

        StageTimelineEmbeddedConfigEditor.DrawScriptableObject(
            phase,
            viewer,
            $"符卡阶段 [{phaseIndex}] · {GetPhaseLabel(encounter, phaseIndex)}",
            defaultExpanded: true);
    }

    public static void DrawPhaseListControls(
        StageTimelineConfigViewer viewer,
        MainBossEncounterConfig encounter,
        int selectedPhaseIndex,
        Action<int> onPhaseSelected)
    {
        if (encounter == null)
            return;

        encounter.bossPhases ??= new System.Collections.Generic.List<BossPhaseConfig>();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("添加符卡阶段", GUILayout.Height(22)))
            {
                var phase = ScriptableObject.CreateInstance<BossPhaseConfig>();
                phase.name = $"BossPhase_{encounter.bossPhases.Count}";
                string folder = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(encounter));
                if (string.IsNullOrEmpty(folder))
                    folder = "Assets/Configs/Boss";

                string path = AssetDatabase.GenerateUniqueAssetPath(
                    $"{folder}/{phase.name}.asset");
                AssetDatabase.CreateAsset(phase, path);
                encounter.bossPhases.Add(phase);
                EditorUtility.SetDirty(encounter);
                onPhaseSelected(encounter.bossPhases.Count - 1);
                viewer?.OnEmbeddedConfigChanged();
            }

            EditorGUI.BeginDisabledGroup(encounter.bossPhases.Count == 0);
            if (GUILayout.Button("删除当前阶段", GUILayout.Height(22)))
            {
                int idx = Mathf.Clamp(selectedPhaseIndex, 0, encounter.bossPhases.Count - 1);
                var removed = encounter.bossPhases[idx];
                encounter.bossPhases.RemoveAt(idx);
                EditorUtility.SetDirty(encounter);
                if (removed != null)
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(removed));

                int next = encounter.bossPhases.Count == 0 ? 0 : Mathf.Min(idx, encounter.bossPhases.Count - 1);
                onPhaseSelected(next);
                viewer?.OnEmbeddedConfigChanged();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
