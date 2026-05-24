using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CharacterConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => characterConfig != null;

    public CharacterConfig CharacterConfig => characterConfig;

    [SerializeField] CharacterConfig characterConfig;

    [Header("信息配置")]
    [SerializeField] E_Character characterName;

    [TextArea(1, 5)]
    [SerializeField] string description;

    [Header("生命配置")]
    [SerializeField] int maxHealth;

    [Header("移速配置")]
    [SerializeField] float speed;
    [SerializeField] float slowSpeed;

    [Header("移动碰撞体配置")]
    [SerializeField] ColliderConfig moveColliderConfig;

    [Header("受击碰撞体配置")]
    [SerializeField] ColliderConfig hitColliderConfig;

    [Header("擦弹碰撞体配置")]
    [SerializeField] ColliderConfig grazeColliderConfig;

#if UNITY_EDITOR
    [Header("武器发射预览")]
    [SerializeField] int previewWeaponIndex;
    [SerializeField] bool previewUseSlowModePrimary;
    [Tooltip("预览模拟时长（秒）")]
    [SerializeField] float previewDuration = 5f;
    [Tooltip("预览弹幕存活时间（秒）")]
    [SerializeField] float previewBulletLifetime = 3f;

    bool _weaponPreviewActive;
    LogicFramePreviewRunner _weaponPreviewClock;
    int _weaponPreviewBulletLifetimeFrames;
    GameObject _weaponPreviewRoot;
    readonly List<WeaponPreviewEmitterState> _weaponPreviewEmitters = new();
    readonly List<PreviewBullet> _weaponPreviewBullets = new();

    sealed class WeaponPreviewEmitterState
    {
        public DanmakuEmitterConfig source;
        public CDanmakuEmitter emitter;
        public uint lastFireFrame;
        public int sequentialBulletIndex;
    }

    struct PreviewBullet
    {
        public GameObject go;
        public float velX;
        public float velY;
        public int ageFrames;
    }
#endif

    public void LoadCharacterConfig() => LoadFromConfig();

    public override void LoadFromConfig()
    {
        if (characterConfig == null)
            return;

        characterName = characterConfig.character;
        description = characterConfig.description;

        maxHealth = characterConfig.maxHealth;

        speed = characterConfig.moveSpeed;
        slowSpeed = characterConfig.moveSlowSpeed;

        moveColliderConfig = characterConfig.moveColliderConfig;
        hitColliderConfig = characterConfig.hitColliderConfig;
        grazeColliderConfig = characterConfig.grazeColliderConfig;

#if UNITY_EDITOR
        ClampPreviewWeaponIndex();
#endif
    }

    public void SaveCharacterConfig()
    {
        if (characterConfig == null) return;

        characterConfig.character = characterName;
        characterConfig.description = description;

        characterConfig.maxHealth = maxHealth;

        characterConfig.moveSpeed = speed;
        characterConfig.moveSlowSpeed = slowSpeed;

        characterConfig.moveColliderConfig = moveColliderConfig;
        characterConfig.hitColliderConfig = hitColliderConfig;
        characterConfig.grazeColliderConfig = grazeColliderConfig;
    }

#if UNITY_EDITOR
    public bool IsPreviewingWeapon => _weaponPreviewActive;

    public int PreviewWeaponCount => characterConfig?.weaponConfigIds?.Length ?? 0;

    public string GetPreviewWeaponLabel(int index)
    {
        if (characterConfig?.weaponConfigIds == null || index < 0 || index >= characterConfig.weaponConfigIds.Length)
            return "(无)";
        string id = characterConfig.weaponConfigIds[index];
        return string.IsNullOrEmpty(id) ? "(空)" : id;
    }

    public void PreviewWeaponFire()
    {
        StartWeaponPreview();
    }

    protected override void StopEditorPreviews() => StopWeaponPreview();

    public void StartWeaponPreview()
    {
        StopWeaponPreview();

        if (characterConfig == null)
        {
            Logger.Warn("[CharacterConfigViewer] 未指定 CharacterConfig，无法预览武器发射。", LogTag.Config);
            return;
        }

        if (characterConfig.weaponConfigIds == null || characterConfig.weaponConfigIds.Length == 0)
        {
            Logger.Warn("[CharacterConfigViewer] 角色未配置 weaponConfigIds。", LogTag.Config);
            return;
        }

        ClampPreviewWeaponIndex();
        string weaponCfgId = characterConfig.weaponConfigIds[previewWeaponIndex];
        var weaponCfg = ConfigViewerAssetLookup.FindWeaponConfig(weaponCfgId);
        if (weaponCfg == null)
        {
            Logger.Warn($"[CharacterConfigViewer] 未找到 WeaponConfig: '{weaponCfgId}'", LogTag.Config);
            return;
        }

        if (!BuildWeaponPreviewEmitters(weaponCfg))
            return;

        uint fps = LogicFramePreviewClock.GetLogicFps();
        _weaponPreviewClock = LogicFramePreviewClock.CreateLogicFrameSession(previewDuration, fps);
        _weaponPreviewClock.Reset();
        _weaponPreviewBulletLifetimeFrames =
            LogicFramePreviewClock.SecondsToLogicFrames(previewBulletLifetime, fps);
        _weaponPreviewActive = true;

        EditorApplication.update -= OnWeaponPreviewUpdate;
        EditorApplication.update += OnWeaponPreviewUpdate;

        Logger.Info(
            $"[CharacterConfigViewer] 武器发射预览开始：{weaponCfg.ConfigId}（{fps} 逻辑FPS，" +
            $"{_weaponPreviewEmitters.Count} 个发射点）。",
            LogTag.Config);
    }

    public void StopWeaponPreview()
    {
        if (!_weaponPreviewActive && _weaponPreviewBullets.Count == 0)
            return;

        _weaponPreviewActive = false;
        EditorApplication.update -= OnWeaponPreviewUpdate;

        ClearWeaponPreviewBullets();
        ConfigViewerEditorScene.DestroyRoot(ref _weaponPreviewRoot);
        _weaponPreviewEmitters.Clear();

        SceneView.RepaintAll();
    }

    void ClampPreviewWeaponIndex()
    {
        int count = PreviewWeaponCount;
        if (count <= 0)
        {
            previewWeaponIndex = 0;
            return;
        }

        previewWeaponIndex = Mathf.Clamp(previewWeaponIndex, 0, count - 1);
    }

    bool BuildWeaponPreviewEmitters(WeaponConfig weaponCfg)
    {
        _weaponPreviewEmitters.Clear();

        var primary = weaponCfg.primaryEmitters.normal;
        string primaryEmitterId = primary.danmakuEmitterConfigId;
        if (previewUseSlowModePrimary)
        {
            string slowId = StringHelper.NormalizeResourceId(
                weaponCfg.primaryEmitters.slowModeDanmakuEmitterConfigId);
            if (!string.IsNullOrEmpty(slowId))
                primaryEmitterId = slowId;
        }

        if (!TryAddWeaponPreviewEmitter(primaryEmitterId, primary.slotOffset))
        {
            Logger.Warn("[CharacterConfigViewer] 主发射器无效，无法开始预览。", LogTag.Config);
            return false;
        }

        var secondarySlots = weaponCfg.secondaryEmitters?.slots;
        if (secondarySlots != null)
        {
            for (int i = 0; i < secondarySlots.Length; i++)
            {
                TryAddWeaponPreviewEmitter(
                    secondarySlots[i].danmakuEmitterConfigId,
                    secondarySlots[i].slotOffset);
            }
        }

        return _weaponPreviewEmitters.Count > 0;
    }

    bool TryAddWeaponPreviewEmitter(string emitterConfigId, Vector2 slotOffset)
    {
        emitterConfigId = StringHelper.NormalizeResourceId(emitterConfigId);
        if (string.IsNullOrEmpty(emitterConfigId))
            return false;

        var emitterCfg = ConfigViewerAssetLookup.FindDanmakuEmitterConfig(emitterConfigId);
        if (emitterCfg == null)
        {
            Logger.Warn(
                $"[CharacterConfigViewer] 未找到 DanmakuEmitterConfig: '{emitterConfigId}'",
                LogTag.Config);
            return false;
        }

        if (emitterCfg.emitMode == EmitMode.None)
        {
            Logger.Warn(
                $"[CharacterConfigViewer] 发射器 '{emitterConfigId}' 模式为 None，已跳过。",
                LogTag.Config);
            return false;
        }

        _weaponPreviewEmitters.Add(new WeaponPreviewEmitterState
        {
            source = emitterCfg,
            emitter = BuildPreviewEmitter(emitterCfg, slotOffset),
            lastFireFrame = 0,
            sequentialBulletIndex = 0,
        });
        return true;
    }

    static CDanmakuEmitter BuildPreviewEmitter(DanmakuEmitterConfig source, Vector2 slotOffset)
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

    void OnWeaponPreviewUpdate()
    {
        if (!_weaponPreviewActive)
            return;

        if (this == null)
        {
            EditorApplication.update -= OnWeaponPreviewUpdate;
            return;
        }

        int steps = _weaponPreviewClock.Tick(out bool stopped);

        for (int s = 0; s < steps; s++)
            StepWeaponPreviewLogicFrame();

        if (stopped)
            StopWeaponPreview();
        else if (steps > 0)
            SceneView.RepaintAll();
    }

    void StepWeaponPreviewLogicFrame()
    {
        AdvanceWeaponPreviewBullets();
        TryFireWeaponPreviewOnLogicFrame();
    }

    void TryFireWeaponPreviewOnLogicFrame()
    {
        uint frame = _weaponPreviewClock.LogicFrame;

        for (int i = 0; i < _weaponPreviewEmitters.Count; i++)
        {
            var state = _weaponPreviewEmitters[i];
            uint since = frame - state.lastFireFrame;
            if (state.emitter.launchCooldownFrames > 0 &&
                since < (uint)state.emitter.launchCooldownFrames)
            {
                continue;
            }

            var danmaku = ResolvePreviewDanmakuConfig(state);
            if (danmaku == null || danmaku.sprite == null)
                continue;

            FireWeaponPreviewBurst(state, danmaku);
            state.lastFireFrame = frame;
        }
    }

    void FireWeaponPreviewBurst(WeaponPreviewEmitterState state, DanmakuConfig danmaku)
    {
        Vector3 origin = transform.position;
        float emitRotRad = transform.eulerAngles.z * Mathf.Deg2Rad + state.emitter.emitterRotOffsetRad;

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
            SpawnWeaponPreviewBullet(danmaku, s.posX, s.posY, s.rotRad, s.velX, s.velY);
        }
    }

    void SpawnWeaponPreviewBullet(DanmakuConfig danmaku, float x, float y, float rotRad, float velX, float velY)
    {
        var go = new GameObject("PreviewDanmaku");
        ConfigViewerEditorScene.AttachTransientObject(go, transform, ref _weaponPreviewRoot, $"{name}_WeaponPreview");

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = danmaku.sprite;
        sr.color = danmaku.color;
        ConfigViewerSpritePreview.ApplySortingFrom(go.transform, transform);

        float scale = Mathf.Max(danmaku.scale, 0.01f);
        go.transform.localScale = Vector3.one * scale;
        go.transform.position = new Vector3(x, y, transform.position.z);
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotRad * Mathf.Rad2Deg);

        _weaponPreviewBullets.Add(new PreviewBullet
        {
            go = go,
            velX = velX,
            velY = velY,
            ageFrames = 0,
        });
    }

    void AdvanceWeaponPreviewBullets()
    {
        for (int i = _weaponPreviewBullets.Count - 1; i >= 0; i--)
        {
            var b = _weaponPreviewBullets[i];
            if (b.go == null)
            {
                _weaponPreviewBullets.RemoveAt(i);
                continue;
            }

            b.ageFrames++;
            if (_weaponPreviewBulletLifetimeFrames > 0 && b.ageFrames >= _weaponPreviewBulletLifetimeFrames)
            {
                DestroyImmediate(b.go);
                _weaponPreviewBullets.RemoveAt(i);
                continue;
            }

            Vector3 p = b.go.transform.position;
            p.x += b.velX;
            p.y += b.velY;
            b.go.transform.position = p;
            _weaponPreviewBullets[i] = b;
        }
    }

    void ClearWeaponPreviewBullets()
    {
        for (int i = 0; i < _weaponPreviewBullets.Count; i++)
        {
            if (_weaponPreviewBullets[i].go != null)
                DestroyImmediate(_weaponPreviewBullets[i].go);
        }

        _weaponPreviewBullets.Clear();
    }

    static DanmakuConfig ResolvePreviewDanmakuConfig(WeaponPreviewEmitterState state)
    {
        string id = ResolvePreviewDanmakuConfigId(state);
        return string.IsNullOrEmpty(id) ? null : ConfigViewerAssetLookup.FindDanmakuConfig(id);
    }

    static string ResolvePreviewDanmakuConfigId(WeaponPreviewEmitterState state)
    {
        if (state.source?.danmakuConfigIds == null || state.source.danmakuConfigIds.Length == 0)
            return null;

        string[] ids = state.source.danmakuConfigIds;
        return state.source.danmakuSelectMode switch
        {
            DanmakuSelectMode.Sequential => PickSequentialPreviewDanmakuId(state, ids),
            DanmakuSelectMode.Random => PickFirstNonEmptyId(ids),
            _ => PickFirstNonEmptyId(ids),
        };
    }

    static string PickSequentialPreviewDanmakuId(WeaponPreviewEmitterState state, string[] ids)
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
#endif

    void OnDrawGizmosSelected()
    {
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, moveColliderConfig, Color.cyan, Color.cyan);
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, hitColliderConfig, Color.red, Color.red);
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, grazeColliderConfig, Color.blue, Color.blue);
    }
}
