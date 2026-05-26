#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 编辑器内武器多发射点弹幕预览（WeaponConfigViewer 等共用）。
/// 须在非 Editor 程序集目录下，供运行时程序集中的 Viewer 在 UNITY_EDITOR 下引用。
/// </summary>
public sealed class ConfigViewerWeaponFirePreview
{
    sealed class EmitterState
    {
        public DanmakuEmitterConfig source;
        public CDanmakuEmitter emitter;
        public uint lastFireFrame;
        public int sequentialBulletIndex;
    }

    struct PreviewBullet
    {
        public GameObject go;
        public DanmakuConfig danmaku;
        public float velX;
        public float velY;
        public int ageFrames;
    }

    readonly List<EmitterState> _emitters = new();
    readonly List<PreviewBullet> _bullets = new();

    Transform _anchor;
    string _rootName;
    bool _active;
    LogicFramePreviewRunner _clock;
    int _bulletLifetimeFrames;
    GameObject _previewRoot;

    public bool IsActive => _active;

    public void Start(
        Transform anchor,
        WeaponConfig weaponConfig,
        int previewPowerOrbs,
        WeaponEditorFirePreviewMode fireMode,
        float previewDurationSeconds,
        float bulletLifetimeSeconds,
        string logTag)
    {
        Stop();

        if (anchor == null || weaponConfig == null || !ConfigViewerEditorScene.CanHostTransientPreview(anchor))
            return;

        _anchor = anchor;
        _rootName = $"{anchor.name}_WeaponPreview";

        bool slowConverge = fireMode == WeaponEditorFirePreviewMode.SlowConverge;
        if (!BuildEmitters(weaponConfig, previewPowerOrbs, slowConverge, logTag))
            return;

        uint fps = LogicFramePreviewClock.GetLogicFps();
        _clock = LogicFramePreviewClock.CreateLogicFrameSession(previewDurationSeconds, fps);
        _clock.Reset();
        _bulletLifetimeFrames = LogicFramePreviewClock.SecondsToLogicFrames(bulletLifetimeSeconds, fps);
        _active = true;

        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
    }

    public void Stop()
    {
        if (!_active && _bullets.Count == 0)
            return;

        _active = false;
        EditorApplication.update -= OnEditorUpdate;

        ClearBullets();
        ConfigViewerEditorScene.DestroyRoot(ref _previewRoot);
        _emitters.Clear();
        _anchor = null;

        SceneView.RepaintAll();
    }

    bool BuildEmitters(WeaponConfig weaponCfg, int previewPowerOrbs, bool slowConverge, string logTag)
    {
        _emitters.Clear();

        string primaryEmitterId = weaponCfg.primaryEmitters.normal.danmakuEmitterConfigId;
        if (slowConverge && weaponCfg.TryGetPrimarySlowSlotForPower(previewPowerOrbs, out var slowSlot))
            primaryEmitterId = slowSlot.danmakuEmitterConfigId;

        Vector2 primaryOffset = weaponCfg.ResolvePrimarySlotOffset(slowConverge, previewPowerOrbs);
        if (!TryAddEmitter(primaryEmitterId, primaryOffset, logTag))
        {
            Logger.Warn($"[{logTag}] 主发射器无效，无法开始预览。", LogTag.Config);
            return false;
        }

        if (weaponCfg.TryGetSecondarySlotsForPower(previewPowerOrbs, out var secondarySlots))
        {
            for (int i = 0; i < secondarySlots.Length; i++)
            {
                Vector2 offset = weaponCfg.ResolveSecondarySlotOffset(secondarySlots[i].slotOffset, slowConverge);
                TryAddEmitter(secondarySlots[i].danmakuEmitterConfigId, offset, logTag);
            }
        }

        return _emitters.Count > 0;
    }

    bool TryAddEmitter(string emitterConfigId, Vector2 slotOffset, string logTag)
    {
        emitterConfigId = StringHelper.NormalizeResourceId(emitterConfigId);
        if (string.IsNullOrEmpty(emitterConfigId))
            return false;

        var emitterCfg = ConfigViewerAssetLookup.FindDanmakuEmitterConfig(emitterConfigId);
        if (emitterCfg == null)
        {
            Logger.Warn($"[{logTag}] 未找到 DanmakuEmitterConfig: '{emitterConfigId}'", LogTag.Config);
            return false;
        }

        if (emitterCfg.emitMode == EmitMode.None)
            return false;

        _emitters.Add(new EmitterState
        {
            source = emitterCfg,
            emitter = BuildEmitter(emitterCfg, slotOffset),
            lastFireFrame = 0,
            sequentialBulletIndex = 0,
        });
        return true;
    }

    static CDanmakuEmitter BuildEmitter(DanmakuEmitterConfig source, Vector2 slotOffset)
    {
        var baked = Object.Instantiate(source);
        uint fps = LogicFramePreviewClock.GetLogicFps();
        baked.BakeLogicTiming(fps);

        var emitter = new CDanmakuEmitter(baked);
        emitter.emitterPosOffsetX += slotOffset.x;
        emitter.emitterPosOffsetY += slotOffset.y;

        Object.DestroyImmediate(baked);
        return emitter;
    }

    void OnEditorUpdate()
    {
        if (!_active)
            return;

        if (_anchor == null || !ConfigViewerEditorScene.CanHostTransientPreview(_anchor))
        {
            Stop();
            return;
        }

        int steps = _clock.Tick(out bool stopped);
        for (int s = 0; s < steps; s++)
            StepLogicFrame();

        if (stopped)
            Stop();
        else if (steps > 0)
            SceneView.RepaintAll();
    }

    void StepLogicFrame()
    {
        AdvanceBullets();
        TryFireOnLogicFrame();
    }

    void TryFireOnLogicFrame()
    {
        uint frame = _clock.LogicFrame;

        for (int i = 0; i < _emitters.Count; i++)
        {
            var state = _emitters[i];
            if (state.emitter.launchCountMax >= 0 &&
                state.emitter.launchCountUsed >= state.emitter.launchCountMax)
            {
                continue;
            }

            uint since = frame - state.lastFireFrame;
            if (state.emitter.launchCooldownFrames > 0 &&
                since < (uint)state.emitter.launchCooldownFrames)
            {
                continue;
            }

            var danmaku = ResolveDanmaku(state);
            if (danmaku == null || danmaku.sprite == null)
                continue;

            FireBurst(state, danmaku);
            state.lastFireFrame = frame;
            state.emitter.launchCountUsed++;
            _emitters[i] = state;
        }
    }

    void FireBurst(EmitterState state, DanmakuConfig danmaku)
    {
        Vector3 origin = _anchor.position;
        float emitRotRad = _anchor.eulerAngles.z * Mathf.Deg2Rad + state.emitter.emitterRotOffsetRad;

        var samples = new List<DanmakuEmitterSpawnMath.SpawnSample>();
        DanmakuEmitterSpawnMath.CollectSpawns(
            in state.emitter,
            origin.x,
            origin.y,
            emitRotRad,
            samples);

        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            SpawnBullet(danmaku, s.posX, s.posY, s.rotRad, s.velX, s.velY);
        }
    }

    void SpawnBullet(DanmakuConfig danmaku, float x, float y, float rotRad, float velX, float velY)
    {
        var go = new GameObject("PreviewDanmaku");
        ConfigViewerEditorScene.AttachTransientObject(go, _anchor, ref _previewRoot, _rootName);

        go.AddComponent<SpriteRenderer>();
        ConfigViewerSpritePreview.ApplySortingFrom(go.transform, _anchor);
        DanmakuPresentation.Apply(danmaku, go);
        go.transform.position = new Vector3(x, y, _anchor.position.z);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotRad * Mathf.Rad2Deg);

        _bullets.Add(new PreviewBullet { go = go, danmaku = danmaku, velX = velX, velY = velY, ageFrames = 0 });
    }

    void AdvanceBullets()
    {
        for (int i = _bullets.Count - 1; i >= 0; i--)
        {
            var b = _bullets[i];
            if (b.go == null)
            {
                _bullets.RemoveAt(i);
                continue;
            }

            Vector3 pos = b.go.transform.position;

            b.ageFrames++;
            if (_bulletLifetimeFrames > 0 && b.ageFrames >= _bulletLifetimeFrames)
            {
                DanmakuHitEffectPresentation.TrySpawnAtConfig(b.danmaku, pos.x, pos.y);
                Object.DestroyImmediate(b.go);
                _bullets.RemoveAt(i);
                continue;
            }

            pos.x += b.velX;
            pos.y += b.velY;
            b.go.transform.position = pos;
            _bullets[i] = b;
        }
    }

    void ClearBullets()
    {
        for (int i = 0; i < _bullets.Count; i++)
        {
            if (_bullets[i].go != null)
                Object.DestroyImmediate(_bullets[i].go);
        }

        _bullets.Clear();
    }

    static DanmakuConfig ResolveDanmaku(EmitterState state)
    {
        string id = ResolveDanmakuId(state);
        return string.IsNullOrEmpty(id) ? null : ConfigViewerAssetLookup.FindDanmakuConfig(id);
    }

    static string ResolveDanmakuId(EmitterState state)
    {
        if (state.source?.danmakuConfigIds == null || state.source.danmakuConfigIds.Length == 0)
            return null;

        string[] ids = state.source.danmakuConfigIds;
        return state.source.danmakuSelectMode switch
        {
            DanmakuSelectMode.Sequential => PickSequentialId(state, ids),
            _ => PickFirstNonEmptyId(ids),
        };
    }

    static string PickSequentialId(EmitterState state, string[] ids)
    {
        for (int attempt = 0; attempt < ids.Length; attempt++)
        {
            int idx = (state.sequentialBulletIndex + attempt) % ids.Length;
            string id = ids[idx];
            if (!string.IsNullOrWhiteSpace(id))
            {
                state.sequentialBulletIndex = idx + 1;
                return id;
            }
        }

        return null;
    }

    static string PickFirstNonEmptyId(string[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(ids[i]))
                return ids[i];
        }

        return null;
    }
}
#endif
