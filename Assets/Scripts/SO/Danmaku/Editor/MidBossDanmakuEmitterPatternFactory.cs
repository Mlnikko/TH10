#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 中场 Boss 专用弹幕发射器（<c>dme_midboss_*</c>），与关底 <c>dme_boss_*</c> 分池。
/// </summary>
public static class MidBossDanmakuEmitterPatternFactory
{
    const string OutputFolder = "Assets/Configs/DanmakuEmitter";
    const string EmitterPrefabId = DanmakuEmitterPrefabArchetypes.Sprite;
    const string BulletBoll = "dm_boll";
    const string BulletStar = "dm_star";

    const float SpeedSlow = 5f;
    const float SpeedNormal = 6f;
    const float SpeedFast = 7f;

    [MenuItem("TH10/弹幕/生成中场 Boss 弹幕发射器配置")]
    public static void CreateAllMidBossPatterns()
    {
        var created = new List<DanmakuEmitterConfig>();

        created.Add(CreateWaveLasher());
        created.Add(CreateFanWide());
        created.Add(CreateFanNarrow());
        created.Add(CreateRing16Rotate());
        created.Add(CreateRing8());
        created.Add(CreateStream());
        created.Add(CreateGrainSpray());
        created.Add(CreateDiagSpread());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AppendToManifest(created);

        Debug.Log($"[MidBossDanmakuEmitterPatternFactory] Created/updated {created.Count} mid-boss emitter configs under {OutputFolder}.");
    }

    static DanmakuEmitterConfig CreateWaveLasher() =>
        Save(CreateBase("DME_MidBoss_Wave_Lasher", BulletBoll, EmitMode.Wave, 0.9f, SpeedNormal, configureWave: wave =>
        {
            wave.centerAngleDeg = -90f;
            wave.swingDegrees = 40f;
            wave.swingHz = 0.45f;
            wave.spreadAngleDeg = 50f;
            wave.bulletCount = 9;
            wave.arcRadius = 0f;
            wave.clockwise = true;
        }));

    static DanmakuEmitterConfig CreateFanWide() =>
        Save(CreateBase("DME_MidBoss_Fan_Wide", BulletBoll, EmitMode.Arc, 0.9f, SpeedNormal, configureArc: arc =>
        {
            arc.arcStartAngle = -128f;
            arc.arcAngle = 76f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 9;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateFanNarrow() =>
        Save(CreateBase("DME_MidBoss_Fan_Narrow", BulletStar, EmitMode.Arc, 0.7f, SpeedFast, configureArc: arc =>
        {
            arc.arcStartAngle = -108f;
            arc.arcAngle = 42f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 7;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateRing16Rotate() =>
        Save(CreateBase("DME_MidBoss_Ring_16_Rotate", BulletBoll, EmitMode.Arc, 1f, SpeedNormal,
            salvoAdvance: 12f, configureArc: arc =>
            {
                arc.arcStartAngle = 0f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.2f;
                arc.arcBulletCount = 16;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateRing8() =>
        Save(CreateBase("DME_MidBoss_Ring_8", BulletStar, EmitMode.Arc, 1.2f, SpeedSlow,
            salvoAdvance: 8f, configureArc: arc =>
            {
                arc.arcStartAngle = 0f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.15f;
                arc.arcBulletCount = 8;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateStream() =>
        Save(CreateBase("DME_MidBoss_Stream", BulletBoll, EmitMode.Line, 0.1f, SpeedNormal, configureLine: line =>
        {
            line.lineDirection = Vector2.down;
            line.lineCount = 1;
            line.lineSpacing = 0f;
        }));

    static DanmakuEmitterConfig CreateGrainSpray() =>
        Save(CreateBase("DME_MidBoss_Grain_Spray", BulletBoll, EmitMode.Grain, 1f, SpeedNormal, configureGrain: grain =>
        {
            grain.bulletCount = 14;
            grain.baseAngleDeg = -90f;
            grain.coneHalfAngleDeg = 36f;
            grain.speedMinScale = 0.8f;
            grain.speedMaxScale = 1.2f;
            grain.spawnScatterRadius = 0.14f;
        }));

    static DanmakuEmitterConfig CreateDiagSpread() =>
        Save(CreateBase("DME_MidBoss_Diag_Spread", BulletBoll, EmitMode.Line, 0.85f, SpeedNormal, configureLine: line =>
        {
            line.lineDirection = new Vector2(0.55f, -0.84f).normalized;
            line.lineCount = 3;
            line.lineSpacing = 0.18f;
        }));

    static DanmakuEmitterConfig CreateBase(
        string assetName,
        string bulletId,
        EmitMode mode,
        float intervalSeconds,
        float speed,
        float salvoAdvance = 0f,
        System.Action<LineModeConfig> configureLine = null,
        System.Action<ArcModeConfig> configureArc = null,
        System.Action<WaveModeConfig> configureWave = null,
        System.Action<GrainModeConfig> configureGrain = null)
    {
        var cfg = ScriptableObject.CreateInstance<DanmakuEmitterConfig>();
        cfg.name = assetName;
        cfg.emitterPrefabId = EmitterPrefabId;
        cfg.danmakuConfigIds = new[] { bulletId };
        cfg.danmakuSelectMode = DanmakuSelectMode.First;
        cfg.emitMode = mode;
        cfg.launchIntervalSeconds = intervalSeconds;
        cfg.launchCount = -1;
        cfg.launchSpeed = speed;
        cfg.salvoAngleAdvanceDeg = salvoAdvance;
        cfg.emitterPosOffset = Vector2.zero;
        cfg.emitterRotOffsetZ = 0f;
        cfg.danmakuRotOffsetZ = 0f;
        cfg.emitterCamp = EmitterCamp.Enemy;
        cfg.audio_Fire = AudioName.None;
        cfg.displayScaleMin = 1f;
        cfg.displayScaleMax = 1f;

        if (configureLine != null)
        {
            var line = cfg.lineModeConfig;
            configureLine(line);
            cfg.lineModeConfig = line;
        }

        if (configureArc != null)
        {
            var arc = cfg.arcModeConfig;
            configureArc(arc);
            cfg.arcModeConfig = arc;
        }

        if (configureWave != null)
        {
            var wave = cfg.waveModeConfig;
            configureWave(wave);
            cfg.waveModeConfig = wave;
        }

        if (configureGrain != null)
        {
            var grain = cfg.grainModeConfig;
            configureGrain(grain);
            cfg.grainModeConfig = grain;
        }

        return cfg;
    }

    static DanmakuEmitterConfig Save(DanmakuEmitterConfig cfg)
    {
        string path = $"{OutputFolder}/{cfg.name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DanmakuEmitterConfig>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(cfg, existing);
            Object.DestroyImmediate(cfg);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        AssetDatabase.CreateAsset(cfg, path);
        return cfg;
    }

    static void AppendToManifest(IReadOnlyList<DanmakuEmitterConfig> configs)
    {
        const string manifestPath = "Assets/Configs/GameResourceManifest.asset";
        var manifest = AssetDatabase.LoadAssetAtPath<GameResourceManifest>(manifestPath);
        if (manifest == null)
        {
            Debug.LogWarning("[MidBossDanmakuEmitterPatternFactory] GameResourceManifest not found.");
            return;
        }

        var ids = new HashSet<string>(manifest.danmakuEmitterConfigIds ?? System.Array.Empty<string>());
        foreach (var cfg in configs)
        {
            if (cfg != null)
                ids.Add(cfg.ConfigId);
        }

        var sorted = new List<string>(ids);
        sorted.Sort();
        manifest.danmakuEmitterConfigIds = sorted.ToArray();
        EditorUtility.SetDirty(manifest);
    }
}
#endif
