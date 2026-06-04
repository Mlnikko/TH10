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
        created.Add(CreateFanDownDouble());
        created.Add(CreateStarPentagonClusterRotate());

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
        { "spell_fan_down_double", "dme_boss_fan_down_double" },
        { "spell_star_pentagon", "dme_boss_star_pentagon_cluster_rotate" },
    };

    static DanmakuEmitterConfig CreateRing24Rotate() =>
        Save(CreateBase("DME_Boss_Ring_24_Rotate", BulletStar, EmitMode.Arc, 1.15f, SpeedNormal,
            presentationDescription: "全周 24 发星弹环，每齐射整体旋转 10°。",
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
            presentationDescription: "全周 32 发玉弹螺旋环，每齐射旋转 15°。",
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
            presentationDescription: "高速 16 发星弹环，每齐射快速旋转 22.5°。",
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
            presentationDescription: "大半径稀疏 14 发星弹环，慢速可读。",
            salvoAdvance: 7.5f, configureArc: arc =>
            {
                arc.arcStartAngle = 0f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.55f;
                arc.arcBulletCount = 14;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateFanOmni() =>
        Save(CreateBase("DME_Boss_Fan_Omni", BulletBoll, EmitMode.Arc, 0.95f, SpeedNormal,
            presentationDescription: "半圆 17 发玉弹扇，覆盖前方大半平面。",
            configureArc: arc =>
        {
            arc.arcStartAngle = -180f;
            arc.arcAngle = 180f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 17;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateFanTight() =>
        Save(CreateBase("DME_Boss_Fan_Tight", BulletStar, EmitMode.Arc, 0.62f, SpeedFast,
            presentationDescription: "窄角 13 发星弹扇，高密度压制。",
            configureArc: arc =>
        {
            arc.arcStartAngle = -108f;
            arc.arcAngle = 36f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 13;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateWaveLasher() =>
        Save(CreateBase("DME_Boss_Wave_Lasher", BulletBoll, EmitMode.Wave, 0.88f, SpeedNormal,
            presentationDescription: "摆扇波弹：11 发扇形随中心角慢摆。",
            configureWave: wave =>
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
        Save(CreateBase("DME_Boss_Wave_Sweep", BulletStar, EmitMode.Wave, 0.72f, SpeedNormal,
            presentationDescription: "宽幅快摆扇：9 发星弹，扫掠感强。",
            configureWave: wave =>
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
            presentationDescription: "摆扇 + 每齐射旋转 14°，螺旋摆扫。",
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
        Save(CreateBase("DME_Boss_Stream", BulletBoll, EmitMode.Line, 0.07f, SpeedNormal,
            presentationDescription: "单发直下流弹，高频率水柱。",
            configureLine: line =>
        {
            line.lineDirection = Vector2.down;
            line.lineCount = 1;
            line.lineSpacing = 0f;
        }));

    static DanmakuEmitterConfig CreateStreamWall() =>
        Save(CreateBase("DME_Boss_Stream_Wall", BulletStar, EmitMode.Line, 0.11f, SpeedBulletHell,
            presentationDescription: "5 发平行流弹墙，高速压场。",
            configureLine: line =>
        {
            line.lineDirection = Vector2.down;
            line.lineCount = 5;
            line.lineSpacing = 0.2f;
        }));

    static DanmakuEmitterConfig CreateGrainStorm() =>
        Save(CreateBase("DME_Boss_Grain_Storm", BulletBoll, EmitMode.Grain, 1.25f, SpeedNormal,
            presentationDescription: "22 发粒弹暴雨，宽锥随机散布。",
            configureGrain: grain =>
        {
            grain.bulletCount = 22;
            grain.baseAngleDeg = -90f;
            grain.coneHalfAngleDeg = 42f;
            grain.speedMinScale = 0.78f;
            grain.speedMaxScale = 1.22f;
            grain.spawnScatterRadius = 0.22f;
        }));

    static DanmakuEmitterConfig CreateGrainCurtain() =>
        Save(CreateBase("DME_Boss_Grain_Curtain", BulletStar, EmitMode.Grain, 0.55f, SpeedFast,
            presentationDescription: "18 发窄锥粒弹幕帘，下落感强。",
            configureGrain: grain =>
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
            presentationDescription: "四向旋转十字弹，每齐射转 11.25°。",
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
            EmitMode.Arc, 1f, SpeedNormal,
            presentationDescription: "20 发双色交替环弹，每齐射旋转 12°。",
            salvoAdvance: 12f, configureArc: arc =>
            {
                arc.arcStartAngle = 0f;
                arc.arcAngle = 360f;
                arc.arcRadius = 0.22f;
                arc.arcBulletCount = 20;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateFanDownDouble() =>
        Save(CreateBase("DME_Boss_Fan_Down_Double", BulletBoll, EmitMode.Arc, 0.5f, SpeedNormal,
            presentationDescription: "向下放射扇形：每 0.5s 一齐射，90° 展开，9 发玉弹。",
            configureArc: arc =>
            {
                arc.arcStartAngle = -135f;
                arc.arcAngle = 90f;
                arc.arcRadius = 0f;
                arc.arcBulletCount = 9;
                arc.arcClockwise = true;
            }));

    static DanmakuEmitterConfig CreateStarPentagonClusterRotate()
    {
        var cfg = CreateBase("DME_Boss_Star_Pentagon_Cluster_Rotate", BulletStar, EmitMode.Arc, 0.1f, SpeedNormal,
            presentationDescription: "五角星旋转：每齐射一角 5 发小扇，齐射角递增 73.2°（五角 + 慢转）。",
            salvoAdvance: 73.2f, configureArc: arc =>
            {
                arc.arcStartAngle = -96f;
                arc.arcAngle = 12f;
                arc.arcRadius = 0f;
                arc.arcBulletCount = 5;
                arc.arcClockwise = true;
            });
        cfg.initialLaunchDelaySeconds = 0.3f;
        return Save(cfg);
    }

    static DanmakuEmitterConfig CreateBase(
        string assetName,
        string bulletId,
        EmitMode mode,
        float intervalSeconds,
        float speed,
        float salvoAdvance = 0f,
        string presentationDescription = null,
        System.Action<LineModeConfig> configureLine = null,
        System.Action<ArcModeConfig> configureArc = null,
        System.Action<WaveModeConfig> configureWave = null,
        System.Action<GrainModeConfig> configureGrain = null) =>
        CreateBase(assetName, new[] { bulletId }, DanmakuSelectMode.First, mode, intervalSeconds, speed,
            salvoAdvance, presentationDescription, configureLine, configureArc, configureWave, configureGrain);

    static DanmakuEmitterConfig CreateBase(
        string assetName,
        string[] bulletIds,
        DanmakuSelectMode selectMode,
        EmitMode mode,
        float intervalSeconds,
        float speed,
        float salvoAdvance = 0f,
        string presentationDescription = null,
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

        if (!string.IsNullOrWhiteSpace(presentationDescription))
            cfg.presentationDescription = presentationDescription.Trim();

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
