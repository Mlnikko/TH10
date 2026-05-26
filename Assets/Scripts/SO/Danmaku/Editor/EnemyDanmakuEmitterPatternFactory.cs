#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成东方 STG 风格的敌人弹幕发射器配置资产（Line / Arc 常用模式）。
/// </summary>
public static class EnemyDanmakuEmitterPatternFactory
{
    const string OutputFolder = "Assets/Configs/DanmakuEmitter";
    const string EmitterPrefabId = "dme_enemy_toplayer";
    const string BulletBoll = "dm_boll";
    const string BulletStar = "dm_star";

    /// <summary>下弹基准速度（世界单位/秒），与道中敌人移动 3.6 同量级。</summary>
    const float SpeedSlow = 5.5f;
    const float SpeedNormal = 7f;
    const float SpeedFast = 8.5f;

    [MenuItem("TH10/Danmaku/Create Enemy Emitter Patterns")]
    public static void CreateAllPatterns()
    {
        var created = new List<DanmakuEmitterConfig>();

        created.Add(CreateLineDownSingle());
        created.Add(CreateLineDownTriple());
        created.Add(CreateLineDownStream());
        created.Add(CreateLineDiagSpread());
        created.Add(CreateArcFanNarrow());
        created.Add(CreateArcFanWide());
        created.Add(CreateArcFanDense());
        created.Add(CreateArcRing8());
        created.Add(CreateArcRing16());
        created.Add(CreateArcRingSpread());
        created.Add(CreateArcFourWay());
        created.Add(CreateWaveFan());
        created.Add(CreateGrainSpray());
        created.Add(CreateGrainDense());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AppendToManifest(created);

        Debug.Log($"[EnemyDanmakuEmitterPatternFactory] Created/updated {created.Count} emitter configs under {OutputFolder}.");
    }

    static DanmakuEmitterConfig CreateLineDownSingle() =>
        Save(CreateBase("DME_Enemy_Down_Single", BulletBoll, EmitMode.Line, 1.2f, SpeedNormal, line =>
        {
            line.lineDirection = Vector2.down;
            line.lineCount = 1;
            line.lineSpacing = 0f;
        }));

    static DanmakuEmitterConfig CreateLineDownTriple() =>
        Save(CreateBase("DME_Enemy_Down_Triple", BulletBoll, EmitMode.Line, 1f, SpeedNormal, line =>
        {
            line.lineDirection = Vector2.down;
            line.lineCount = 3;
            line.lineSpacing = 0.22f;
        }));

    static DanmakuEmitterConfig CreateLineDownStream() =>
        Save(CreateBase("DME_Enemy_Down_Stream", BulletBoll, EmitMode.Line, 0.14f, SpeedSlow, line =>
        {
            line.lineDirection = Vector2.down;
            line.lineCount = 1;
            line.lineSpacing = 0f;
        }));

    /// <summary>双路斜下（左/右各一发），常见于妖精俯冲。</summary>
    static DanmakuEmitterConfig CreateLineDiagSpread() =>
        Save(CreateBase("DME_Enemy_Diag_Spread", BulletBoll, EmitMode.Line, 0.95f, SpeedNormal, line =>
        {
            line.lineDirection = new Vector2(0.55f, -0.84f).normalized;
            line.lineCount = 2;
            line.lineSpacing = 0.2f;
        }));

    static DanmakuEmitterConfig CreateArcFanNarrow() =>
        Save(CreateBase("DME_Enemy_Fan_Narrow", BulletBoll, EmitMode.Arc, 0.85f, SpeedNormal, null, arc =>
        {
            arc.arcStartAngle = -115f;
            arc.arcAngle = 50f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 5;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateArcFanWide() =>
        Save(CreateBase("DME_Enemy_Fan_Wide", BulletBoll, EmitMode.Arc, 1.05f, SpeedNormal, null, arc =>
        {
            arc.arcStartAngle = -135f;
            arc.arcAngle = 90f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 9;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateArcFanDense() =>
        Save(CreateBase("DME_Enemy_Fan_Dense", BulletStar, EmitMode.Arc, 0.55f, SpeedFast, null, arc =>
        {
            arc.arcStartAngle = -108f;
            arc.arcAngle = 36f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 7;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateArcRing8() =>
        Save(CreateBase("DME_Enemy_Ring_8", BulletBoll, EmitMode.Arc, 1.35f, SpeedSlow, null, arc =>
        {
            arc.arcStartAngle = 0f;
            arc.arcAngle = 360f;
            arc.arcRadius = 0.15f;
            arc.arcBulletCount = 8;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateArcRing16() =>
        Save(CreateBase("DME_Enemy_Ring_16", BulletStar, EmitMode.Arc, 1f, SpeedNormal, null, arc =>
        {
            arc.arcStartAngle = 0f;
            arc.arcAngle = 360f;
            arc.arcRadius = 0.2f;
            arc.arcBulletCount = 16;
            arc.arcClockwise = true;
        }));

    /// <summary>带发射半径的扩散环（经典「开花开阔」观感）。</summary>
    static DanmakuEmitterConfig CreateArcRingSpread() =>
        Save(CreateBase("DME_Enemy_Ring_Spread", BulletStar, EmitMode.Arc, 1.2f, SpeedSlow, null, arc =>
        {
            arc.arcStartAngle = 0f;
            arc.arcAngle = 360f;
            arc.arcRadius = 0.35f;
            arc.arcBulletCount = 12;
            arc.arcClockwise = true;
        }));

    static DanmakuEmitterConfig CreateArcFourWay() =>
        Save(CreateBase("DME_Enemy_Four_Way", BulletBoll, EmitMode.Arc, 1.5f, SpeedNormal, null, arc =>
        {
            arc.arcStartAngle = 45f;
            arc.arcAngle = 360f;
            arc.arcRadius = 0f;
            arc.arcBulletCount = 4;
            arc.arcClockwise = true;
        }));

    /// <summary>摆动扇形波弹（东方摆 N-Way）。</summary>
    static DanmakuEmitterConfig CreateWaveFan() =>
        Save(CreateBase("DME_Enemy_Wave_Fan", BulletBoll, EmitMode.Wave, 0.75f, SpeedNormal, null, null,
            wave =>
            {
                wave.centerAngleDeg = -90f;
                wave.swingDegrees = 32f;
                wave.swingHz = 0.5f;
                wave.spreadAngleDeg = 48f;
                wave.bulletCount = 7;
                wave.arcRadius = 0f;
                wave.clockwise = true;
            }));

    /// <summary>锥形粒弹散布。</summary>
    static DanmakuEmitterConfig CreateGrainSpray() =>
        Save(CreateBase("DME_Enemy_Grain_Spray", BulletBoll, EmitMode.Grain, 1.1f, SpeedNormal, null, null, null,
            grain =>
            {
                grain.bulletCount = 10;
                grain.baseAngleDeg = -90f;
                grain.coneHalfAngleDeg = 32f;
                grain.speedMinScale = 0.8f;
                grain.speedMaxScale = 1.2f;
                grain.spawnScatterRadius = 0.12f;
            }));

    /// <summary>高密度粒弹（精英撒弹）。</summary>
    static DanmakuEmitterConfig CreateGrainDense() =>
        Save(CreateBase("DME_Enemy_Grain_Dense", BulletStar, EmitMode.Grain, 0.65f, SpeedFast, null, null, null,
            grain =>
            {
                grain.bulletCount = 16;
                grain.baseAngleDeg = -90f;
                grain.coneHalfAngleDeg = 40f;
                grain.speedMinScale = 0.75f;
                grain.speedMaxScale = 1.25f;
                grain.spawnScatterRadius = 0.18f;
            }));

    static DanmakuEmitterConfig CreateBase(
        string assetName,
        string bulletId,
        EmitMode mode,
        float intervalSeconds,
        float speed,
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
            Debug.LogWarning("[EnemyDanmakuEmitterPatternFactory] GameResourceManifest not found.");
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
