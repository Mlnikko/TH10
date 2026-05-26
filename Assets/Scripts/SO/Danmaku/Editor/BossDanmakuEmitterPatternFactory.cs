#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成关底 Boss 专用弹幕发射器（dme_boss_*）。中场 Boss 见 MidBossDanmakuEmitterPatternFactory。
/// </summary>
public static class BossDanmakuEmitterPatternFactory
{
    const string OutputFolder = "Assets/Configs/DanmakuEmitter";
    const string EmitterPrefabId = DanmakuEmitterPrefabArchetypes.Sprite;
    const string BulletBoll = "dm_boll";
    const string BulletStar = "dm_star";

    /// <summary>Boss 弹速略低于道中，便于读弹。</summary>
    const float SpeedSlow = 4.5f;
    const float SpeedNormal = 5.5f;
    const float SpeedFast = 6.5f;
    const float SpeedBulletHell = 7.2f;

    [MenuItem("TH10/弹幕/生成关底 Boss 弹幕发射器配置")]
    public static void CreateAllBossPatterns()
    {
        var created = new List<DanmakuEmitterConfig>();

        created.Add(CreateRing24Rotate());
        created.Add(CreateRing32Spiral());
        created.Add(CreateRing16Fast());
        created.Add(CreateRingSpreadHuge());
        created.Add(CreateFanOmni());
        created.Add(CreateFanTight());
        created.Add(CreateWaveLasher());
        created.Add(CreateWaveSweep());
        created.Add(CreateWaveRotate());
        created.Add(CreateStream());
        created.Add(CreateStreamWall());
        created.Add(CreateGrainStorm());
        created.Add(CreateGrainCurtain());
        created.Add(CreateFourWayRotate());
        created.Add(CreateRingDualColor());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AppendToManifest(created);

        Debug.Log($"[BossDanmakuEmitterPatternFactory] Created/updated {created.Count} boss emitter configs under {OutputFolder}.");
    }

    /// <summary>符卡阶段推荐映射（供 BossPhaseConfig.spellCardId 或 EnemyConfig.emitterConfigId 引用）。</summary>
    public static IReadOnlyDictionary<string, string> RecommendedSpellCardEmitters => new Dictionary<string, string>
    {
        { "spell_ring_rotate", "dme_boss_ring_24_rotate" },
        { "spell_spiral", "dme_boss_ring_32_spiral" },
        { "spell_fan_omni", "dme_boss_fan_omni" },
        { "spell_wave_lasher", "dme_boss_wave_lasher" },
        { "spell_grain_storm", "dme_boss_grain_storm" },
        { "spell_stream_wall", "dme_boss_stream_wall" },
        { "spell_four_way", "dme_boss_four_way_rotate" },
    };

    static DanmakuEmitterConfig CreateRing24Rotate() =>
        Save(CreateBase("DME_Boss_Ring_24_Rotate", BulletStar, EmitMode.Arc, 1.15f, SpeedNormal,
            salvoAdvance: 10f, configureArc: arc =>
            {
                arc.arcStartAngle = 0f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.28f;
                arc.arcBulletCount = 24;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateRing32Spiral() =>
        Save(CreateBase("DME_Boss_Ring_32_Spiral", BulletBoll, EmitMode.Arc, 0.85f, SpeedSlow,
            salvoAdvance: 15f, configureArc: arc =>
            {
                arc.arcStartAngle = 0f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.18f;
                arc.arcBulletCount = 32;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateRing16Fast() =>
        Save(CreateBase("DME_Boss_Ring_16_Fast", BulletStar, EmitMode.Arc, 0.5f, SpeedFast,
            salvoAdvance: 22.5f, configureArc: arc =>
            {
                arc.arcStartAngle = 0f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.15f;
                arc.arcBulletCount = 16;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateRingSpreadHuge() =>
        Save(CreateBase("DME_Boss_Ring_Spread", BulletStar, EmitMode.Arc, 1.35f, SpeedSlow,
            salvoAdvance: 7.5f, configureArc: arc =>
            {
                arc.arcStartAngle = 0f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.55f;
                arc.arcBulletCount = 14;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateFanOmni() =>
        Save(CreateBase("DME_Boss_Fan_Omni", BulletBoll, EmitMode.Arc, 0.95f, SpeedNormal, configureArc: arc =>
        {
            arc.arcStartAngle = -180f;
            arc.arcAngle = 180f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 17;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateFanTight() =>
        Save(CreateBase("DME_Boss_Fan_Tight", BulletStar, EmitMode.Arc, 0.62f, SpeedFast, configureArc: arc =>
        {
            arc.arcStartAngle = -108f;
            arc.arcAngle = 36f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 13;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateWaveLasher() =>
        Save(CreateBase("DME_Boss_Wave_Lasher", BulletBoll, EmitMode.Wave, 0.88f, SpeedNormal, configureWave: wave =>
        {
            wave.centerAngleDeg = -90f;
            wave.swingDegrees = 48f;
            wave.swingHz = 0.4f;
            wave.spreadAngleDeg = 56f;
            wave.bulletCount = 11;
            wave.arcRadius = 0f;
            wave.clockwise = true;
        }));

    static DanmakuEmitterConfig CreateWaveSweep() =>
        Save(CreateBase("DME_Boss_Wave_Sweep", BulletStar, EmitMode.Wave, 0.72f, SpeedNormal, configureWave: wave =>
        {
            wave.centerAngleDeg = -90f;
            wave.swingDegrees = 60f;
            wave.swingHz = 1f;
            wave.spreadAngleDeg = 72f;
            wave.bulletCount = 9;
            wave.arcRadius = 0f;
            wave.clockwise = true;
        }));

    static DanmakuEmitterConfig CreateWaveRotate() =>
        Save(CreateBase("DME_Boss_Wave_Rotate", BulletBoll, EmitMode.Wave, 0.8f, SpeedNormal,
            salvoAdvance: 14f, configureWave: wave =>
            {
                wave.centerAngleDeg = -90f;
                wave.swingDegrees = 36f;
                wave.swingHz = 0.65f;
                wave.spreadAngleDeg = 44f;
                wave.bulletCount = 9;
                wave.arcRadius = 0f;
                wave.clockwise = true;
            }));

    static DanmakuEmitterConfig CreateStream() =>
        Save(CreateBase("DME_Boss_Stream", BulletBoll, EmitMode.Line, 0.07f, SpeedNormal, configureLine: line =>
        {
            line.lineDirection = Vector2.down;
            line.lineCount = 1;
            line.lineSpacing = 0f;
        }));

    static DanmakuEmitterConfig CreateStreamWall() =>
        Save(CreateBase("DME_Boss_Stream_Wall", BulletStar, EmitMode.Line, 0.11f, SpeedBulletHell, configureLine: line =>
        {
            line.lineDirection = Vector2.down;
            line.lineCount = 5;
            line.lineSpacing = 0.2f;
        }));

    static DanmakuEmitterConfig CreateGrainStorm() =>
        Save(CreateBase("DME_Boss_Grain_Storm", BulletBoll, EmitMode.Grain, 1.25f, SpeedNormal, configureGrain: grain =>
        {
            grain.bulletCount = 22;
            grain.baseAngleDeg = -90f;
            grain.coneHalfAngleDeg = 42f;
            grain.speedMinScale = 0.78f;
            grain.speedMaxScale = 1.22f;
            grain.spawnScatterRadius = 0.22f;
        }));

    static DanmakuEmitterConfig CreateGrainCurtain() =>
        Save(CreateBase("DME_Boss_Grain_Curtain", BulletStar, EmitMode.Grain, 0.55f, SpeedFast, configureGrain: grain =>
        {
            grain.bulletCount = 18;
            grain.baseAngleDeg = -90f;
            grain.coneHalfAngleDeg = 24f;
            grain.speedMinScale = 0.72f;
            grain.speedMaxScale = 1.28f;
            grain.spawnScatterRadius = 0.15f;
        }));

    static DanmakuEmitterConfig CreateFourWayRotate() =>
        Save(CreateBase("DME_Boss_Four_Way_Rotate", BulletBoll, EmitMode.Arc, 1.1f, SpeedNormal,
            salvoAdvance: 11.25f, configureArc: arc =>
            {
                arc.arcStartAngle = 45f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.12f;
                arc.arcBulletCount = 4;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateRingDualColor() =>
        Save(CreateBase("DME_Boss_Ring_Dual", new[] { BulletBoll, BulletStar }, DanmakuSelectMode.Sequential,
            EmitMode.Arc, 1f, SpeedNormal, salvoAdvance: 12f, configureArc: arc =>
            {
                arc.arcStartAngle = 0f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.22f;
                arc.arcBulletCount = 20;
                arc.arcClockwise = true;
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
        System.Action<GrainModeConfig> configureGrain = null) =>
        CreateBase(assetName, new[] { bulletId }, DanmakuSelectMode.First, mode, intervalSeconds, speed,
            salvoAdvance, configureLine, configureArc, configureWave, configureGrain);

    static DanmakuEmitterConfig CreateBase(
        string assetName,
        string[] bulletIds,
        DanmakuSelectMode selectMode,
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
        cfg.danmakuConfigIds = bulletIds;
        cfg.danmakuSelectMode = selectMode;
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
            Debug.LogWarning("[BossDanmakuEmitterPatternFactory] GameResourceManifest not found.");
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
