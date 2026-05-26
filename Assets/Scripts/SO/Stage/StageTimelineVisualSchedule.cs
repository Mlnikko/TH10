using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 关卡时间轴可视化：根据各配置的「开始时刻 + 内部时长」构建时间线条目（编辑器时间线 UI 与预览估算共用）。
/// </summary>
public static class StageTimelineVisualSchedule
{
    public enum ClipKind : byte
    {
        MidStageWave = 0,
        MidBoss = 1,
        MainBoss = 2,
    }

    public readonly struct Clip
    {
        public Clip(
            ClipKind kind,
            int index,
            string label,
            float startSeconds,
            float durationSeconds,
            bool waitForClear,
            UnityEngine.Object undoTarget)
        {
            Kind = kind;
            Index = index;
            Label = label;
            StartSeconds = startSeconds;
            DurationSeconds = durationSeconds;
            WaitForClear = waitForClear;
            UndoTarget = undoTarget;
        }

        public ClipKind Kind { get; }
        public int Index { get; }
        public string Label { get; }
        public float StartSeconds { get; }
        public float DurationSeconds { get; }
        public bool WaitForClear { get; }
        public UnityEngine.Object UndoTarget { get; }

        public float EndSeconds => StartSeconds + DurationSeconds;
    }

    const float MinClipDurationSeconds = 0.25f;
    const float AutoFitPaddingSeconds = 8f;

    public static float ResolveTimelineDurationSeconds(
        StageTimelineConfig timeline,
        float viewDurationOverrideSeconds,
        uint logicFps)
    {
        if (viewDurationOverrideSeconds > 0f)
            return viewDurationOverrideSeconds;

        if (timeline != null && timeline.maxStageDurationSeconds > 0f)
            return timeline.maxStageDurationSeconds;

        float autoEnd = ComputeContentEndSeconds(timeline, logicFps);
        return Mathf.Max(30f, autoEnd + AutoFitPaddingSeconds);
    }

    public static void CollectClips(StageTimelineConfig timeline, uint logicFps, List<Clip> clips)
    {
        clips?.Clear();
        if (timeline == null || clips == null)
            return;

        if (timeline.midStageWaves != null)
        {
            for (int i = 0; i < timeline.midStageWaves.Count; i++)
            {
                var wave = timeline.midStageWaves[i];
                if (wave == null)
                    continue;

                clips.Add(new Clip(
                    ClipKind.MidStageWave,
                    i,
                    BuildWaveLabel(wave, i),
                    Mathf.Max(0f, wave.startTimeSeconds),
                    EstimateWaveDurationSeconds(wave, logicFps),
                    wave.waitForClear,
                    wave));
            }
        }

        var mid = timeline.midBossEncounter;
        if (mid != null && mid.enabled)
        {
            clips.Add(new Clip(
                ClipKind.MidBoss,
                0,
                string.IsNullOrEmpty(mid.name) ? "中场 Boss" : mid.name,
                Mathf.Max(0f, mid.spawnTimeSeconds),
                EstimateMidBossDurationSeconds(mid, logicFps),
                false,
                mid));
        }

        var main = timeline.mainBossEncounter;
        if (main != null && main.enabled)
        {
            clips.Add(new Clip(
                ClipKind.MainBoss,
                0,
                string.IsNullOrEmpty(main.name) ? "关底 Boss" : main.name,
                Mathf.Max(0f, main.spawnTimeSeconds),
                EstimateMainBossDurationSeconds(main, logicFps),
                false,
                main));
        }
    }

    public static float EstimateWaveDurationSeconds(EnemyWaveConfig wave, uint logicFps)
    {
        if (wave == null)
            return MinClipDurationSeconds;

        float spawnSpan = EstimateWaveSpawnSpanSeconds(wave);
        float pathDur = EstimateWavePathDurationSeconds(wave, logicFps);
        return Mathf.Max(MinClipDurationSeconds, spawnSpan + pathDur);
    }

    public static float EstimateMidBossDurationSeconds(MidBossEncounterConfig encounter, uint logicFps)
    {
        if (encounter == null)
            return MinClipDurationSeconds;

        float entry = EstimateMovementDurationSeconds(encounter.entryPathRoute, logicFps);
        float onField = Mathf.Max(0f, encounter.onFieldDurationSeconds);
        float exit = EstimateMovementDurationSeconds(encounter.exitPathRoute, logicFps);
        return Mathf.Max(MinClipDurationSeconds, entry + onField + exit);
    }

    public static float EstimateMainBossDurationSeconds(MainBossEncounterConfig encounter, uint logicFps)
    {
        if (encounter == null)
            return MinClipDurationSeconds;

        float entry = EstimateMovementDurationSeconds(encounter.entryPathRoute, logicFps);
        float intro = Mathf.Max(0f, encounter.bossIntroDurationSeconds);
        float fight = EstimateMainBossFightSpanSeconds(encounter, logicFps);
        return Mathf.Max(MinClipDurationSeconds, entry + intro + fight);
    }

    public static float EstimateMovementDurationSeconds(PathRouteMovementData pathRoute, uint logicFps)
    {
        if (pathRoute == null)
            return 0f;

        pathRoute.BakeMovementTiming(logicFps);
        var baked = EnemyPathMovementBaking.BakeRoute(pathRoute, logicFps);
        if (baked.durationFrames > 0)
            return baked.durationFrames / Mathf.Max(1f, logicFps);

        if (pathRoute.durationSeconds >= 0f)
            return pathRoute.durationSeconds;

        return 0f;
    }

    public static void ApplyClipStartSeconds(Clip clip, float startSeconds, uint logicFps)
    {
        startSeconds = Mathf.Max(0f, startSeconds);
        switch (clip.Kind)
        {
            case ClipKind.MidStageWave:
                if (clip.UndoTarget is EnemyWaveConfig wave)
                {
                    wave.startTimeSeconds = startSeconds;
                    wave.BakeLogicTiming(logicFps);
                }
                break;
            case ClipKind.MidBoss:
                if (clip.UndoTarget is MidBossEncounterConfig mid)
                {
                    mid.spawnTimeSeconds = startSeconds;
                    mid.BakeLogicTiming(logicFps);
                }
                break;
            case ClipKind.MainBoss:
                if (clip.UndoTarget is MainBossEncounterConfig main)
                {
                    main.spawnTimeSeconds = startSeconds;
                    main.BakeLogicTiming(logicFps);
                }
                break;
        }
    }

    static float ComputeContentEndSeconds(StageTimelineConfig timeline, uint logicFps)
    {
        var buffer = new List<Clip>(16);
        CollectClips(timeline, logicFps, buffer);
        float end = 0f;
        for (int i = 0; i < buffer.Count; i++)
            end = Mathf.Max(end, buffer[i].EndSeconds);
        return end;
    }

    static float EstimateWaveSpawnSpanSeconds(EnemyWaveConfig wave)
    {
        wave.EnsureSpawnQueueMigrated();
        int count = wave.ResolveSpawnCount();
        if (!wave.UsesSequentialSpawn || count <= 1)
            return 0f;

        float total = 0f;
        for (int i = 1; i < count; i++)
        {
            float delay = wave.spawnQueue[i].delayAfterPreviousSeconds;
            if (delay <= 0f)
                delay = wave.spawnIntervalSeconds;
            total += Mathf.Max(0f, delay);
        }

        return total;
    }

    static float EstimateWavePathDurationSeconds(EnemyWaveConfig wave, uint logicFps)
    {
        float maxDur = 0f;
        wave.EnsureSpawnQueueMigrated();
        int count = Mathf.Max(1, wave.ResolveSpawnCount());

        if (wave.UsesPerQueueEntryPaths)
        {
            for (int i = 0; i < count; i++)
            {
                var route = wave.ResolveEffectivePathRoute(i);
                maxDur = Mathf.Max(maxDur, EstimateMovementDurationSeconds(route, logicFps));
            }
        }
        else
        {
            maxDur = EstimateMovementDurationSeconds(wave.pathRoute, logicFps);
        }

        if (maxDur <= 0f && wave.useDefaultDescentIfNoMovement)
        {
            var area = BattleAreaData.Default;
            float descent = Mathf.Max(0.01f, wave.defaultDescentSpeed);
            maxDur = area.Height / descent;
        }

        float hold = 0f;
        if (wave.pathRoute != null)
            hold = Mathf.Max(0f, wave.pathRoute.spawnHoldSeconds);

        return maxDur + hold;
    }

    static float EstimateMainBossFightSpanSeconds(MainBossEncounterConfig encounter, uint logicFps)
    {
        float span = 15f;
        if (encounter.bossPhases == null || encounter.bossPhases.Count == 0)
            return span;

        for (int i = 0; i < encounter.bossPhases.Count; i++)
        {
            var phase = encounter.bossPhases[i];
            if (phase == null || phase.triggerType != BossPhaseConfig.TriggerType.Time)
                continue;

            float phaseEnd = Mathf.Max(0f, phase.triggerTimeSeconds);
            if (phase.durationSeconds >= 0f)
                phaseEnd += phase.durationSeconds;
            else
                phaseEnd += 30f;

            span = Mathf.Max(span, phaseEnd);
        }

        return span;
    }

    static string BuildWaveLabel(EnemyWaveConfig wave, int index)
    {
        if (!string.IsNullOrEmpty(wave.name))
            return $"波次 {index}: {wave.name}";
        return $"波次 {index}";
    }
}
