using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// 战斗区背景运行时：多层平面背景循环滚动、云雾层、Boss 击败 Shake。
/// 由 <see cref="StageTimelineConfigViewer"/> 在 Play 模式预览，或由
/// <see cref="BattleStageBackgroundPresenter"/> 在正式战斗中驱动。
/// </summary>
[DisallowMultipleComponent]
public class BattleStageBackgroundRuntime : MonoBehaviour
{
    const string BackgroundShaderName = "TH10/BattleBackgroundScroll";

    static readonly int ScrollId = Shader.PropertyToID("_Scroll");

    [SerializeField] Transform shakeRoot;
    [SerializeField] Transform backgroundRoot;
    [SerializeField] Transform cloudsRoot;

    BattleAreaBackgroundData _data;
    BattleAreaData _area;
    bool _active;
    bool _simulationEnabled;
    bool _cloudUsePool;
    int _cloudPrefabPoolIndex = -1;

    Mesh _sharedBackgroundMesh;
    readonly List<BackgroundLayerRuntime> _backgroundLayers = new();
    readonly List<CloudLayerRuntime> _cloudLayers = new();

    Transform _shakeTarget;
    Vector3 _restShakeLocalPosition;
    Tween _activeShake;

    readonly struct ShakeProfile
    {
        public readonly float Duration;
        public readonly float Strength;
        public readonly int Vibrato;

        public ShakeProfile(float duration, float strength, int vibrato)
        {
            Duration = duration;
            Strength = strength;
            Vibrato = vibrato;
        }
    }

    static readonly ShakeProfile MidBossShake = new(0.45f, 0.12f, 18);
    static readonly ShakeProfile MainBossShake = new(0.75f, 0.22f, 24);

    sealed class BackgroundLayerRuntime
    {
        public Material material;
        public Vector2 scrollOffsetUv;
        public Vector2 scrollUvPerSecond;
    }

    sealed class CloudLayerRuntime
    {
        public BattleAreaCloudLayerData config;
        public Sprite sprite;
        public float spawnTimer;
        public readonly List<CloudInstance> instances = new();
    }

    sealed class CloudInstance
    {
        public GameObject gameObject;
        public float fallSpeed;
        public float recycleBottomY;
        public float halfHeight;
    }

    public bool IsActive => _active;

    public void Apply(
        in BattleAreaData area,
        BattleAreaBackgroundData data,
        System.Func<string, Sprite> spriteResolver)
    {
        ClearVisualsInternal();

        _area = area;
        _data = data ?? new BattleAreaBackgroundData();

        if (!_data.enabled || spriteResolver == null)
            return;

        EnsureHierarchy();
        BuildBackgroundLayers(spriteResolver);
        BuildCloudLayers(spriteResolver);

        _active = _backgroundLayers.Count > 0 || _cloudLayers.Count > 0;
        if (!_active)
            return;

        transform.position = new Vector3(_area.Center.x, _area.Center.y, 0f);
        _shakeTarget = shakeRoot != null ? shakeRoot : transform;
        _restShakeLocalPosition = _shakeTarget.localPosition;
        _simulationEnabled = true;
    }

    public void ClearVisuals()
    {
        _simulationEnabled = false;
        ClearVisualsInternal();
    }

    public void DisposeInstance()
    {
        ClearVisualsInternal();
        DestroyGameObjectSafe(gameObject);
    }

    public void TryShakeMidBossDefeated() => TryShake(MidBossShake);

    public void TryShakeMainBossDefeated() => TryShake(MainBossShake);

    void Update()
    {
        if (!Application.isPlaying)
            return;

        TickSimulation(Time.deltaTime);
    }

    void TickSimulation(float deltaTime)
    {
        if (!_active || !_simulationEnabled || deltaTime <= 0f)
            return;

        TickBackgroundScroll(deltaTime);
        TickClouds(deltaTime);
    }

    void TickBackgroundScroll(float deltaTime)
    {
        for (int i = 0; i < _backgroundLayers.Count; i++)
        {
            var layer = _backgroundLayers[i];
            if (layer.material == null || layer.scrollUvPerSecond.sqrMagnitude < 0.000001f)
                continue;

            layer.scrollOffsetUv += layer.scrollUvPerSecond * deltaTime;
            layer.scrollOffsetUv.x = Mathf.Repeat(layer.scrollOffsetUv.x, 1f);
            layer.scrollOffsetUv.y = Mathf.Repeat(layer.scrollOffsetUv.y, 1f);
            layer.material.SetVector(ScrollId, new Vector4(layer.scrollOffsetUv.x, layer.scrollOffsetUv.y, 0f, 0f));
        }
    }

    void TickClouds(float deltaTime)
    {
        if (_cloudLayers.Count == 0)
            return;

        float topY = _area.Top + _area.Height * 0.15f;

        for (int li = 0; li < _cloudLayers.Count; li++)
        {
            var layer = _cloudLayers[li];
            if (layer.sprite == null)
                continue;

            layer.spawnTimer -= deltaTime;
            if (layer.spawnTimer <= 0f && layer.instances.Count < layer.config.maxActiveCount)
            {
                SpawnCloud(layer, topY);
                layer.spawnTimer = layer.config.spawnIntervalSeconds * Random.Range(0.75f, 1.25f);
            }

            for (int i = layer.instances.Count - 1; i >= 0; i--)
            {
                var inst = layer.instances[i];
                if (inst.gameObject == null)
                {
                    layer.instances.RemoveAt(i);
                    continue;
                }

                var pos = inst.gameObject.transform.localPosition;
                pos.y -= inst.fallSpeed * deltaTime;
                inst.gameObject.transform.localPosition = pos;

                if (pos.y < inst.recycleBottomY - inst.halfHeight)
                {
                    RecycleCloudInstance(inst);
                    layer.instances.RemoveAt(i);
                }
            }
        }
    }

    static bool ShouldUseCloudPool() =>
        Application.isPlaying
        && GameObjectPoolManager.Instance != null
        && GameResDB.IsInitialized;

    void EnsureHierarchy()
    {
        if (shakeRoot == null)
        {
            var shakeGo = new GameObject("ShakeRoot");
            shakeGo.transform.SetParent(transform, false);
            shakeRoot = shakeGo.transform;
        }

        if (backgroundRoot == null)
        {
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(shakeRoot, false);
            backgroundRoot = bgGo.transform;
        }

        if (cloudsRoot == null)
        {
            var cloudGo = new GameObject("Clouds");
            cloudGo.transform.SetParent(shakeRoot, false);
            cloudsRoot = cloudGo.transform;
        }
    }

    void BuildBackgroundLayers(System.Func<string, Sprite> spriteResolver)
    {
        _backgroundLayers.Clear();

        if (_data.backgroundLayers == null || _data.backgroundLayers.Length == 0)
            return;

        var shader = GameResDB.IsInitialized
            ? GameResDB.Instance.BattleBackgroundScrollShader
            : null;
        if (shader == null)
            shader = Shader.Find(BackgroundShaderName);
        if (shader == null)
        {
            Logger.Warn($"[BattleStageBackground] Shader '{BackgroundShaderName}' not found.", LogTag.Battle);
            return;
        }

        _sharedBackgroundMesh = BuildFlatMesh(_area.Width, _area.Height);

        for (int i = 0; i < _data.backgroundLayers.Length; i++)
        {
            var cfg = _data.backgroundLayers[i];
            if (cfg == null || string.IsNullOrEmpty(cfg.textureId))
                continue;

            var sprite = spriteResolver(cfg.textureId);
            if (sprite == null)
                continue;

            ComputeCoverMetrics(sprite, _area.Width, _area.Height, out float worldWidth, out float worldHeight);

            var bgGo = new GameObject($"ScrollLayer_{cfg.textureId}");
            bgGo.transform.SetParent(backgroundRoot, false);

            var meshFilter = bgGo.AddComponent<MeshFilter>();
            var meshRenderer = bgGo.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = _sharedBackgroundMesh;

            var material = new Material(shader) { mainTexture = sprite.texture };
            if (cfg.alpha < 1f)
            {
                var color = material.color;
                color.a = cfg.alpha;
                material.color = color;
            }

            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = cfg.sortingOrder;

            Vector2 scrollUvPerSecond = Vector2.zero;
            Vector2 scrollDir = cfg.scroll.NormalizedDirection;
            if (scrollDir.sqrMagnitude > 0.0001f && cfg.scroll.speed > 0f)
            {
                scrollUvPerSecond = new Vector2(
                    scrollDir.x * cfg.scroll.speed / Mathf.Max(0.001f, worldWidth),
                    scrollDir.y * cfg.scroll.speed / Mathf.Max(0.001f, worldHeight));
            }

            material.SetVector(ScrollId, Vector4.zero);

            _backgroundLayers.Add(new BackgroundLayerRuntime
            {
                material = material,
                scrollOffsetUv = Vector2.zero,
                scrollUvPerSecond = scrollUvPerSecond,
            });
        }
    }

    static void ComputeCoverMetrics(Sprite sprite, float areaWidth, float areaHeight, out float worldWidth, out float worldHeight)
    {
        Vector2 spriteSize = sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f || areaWidth <= 0f || areaHeight <= 0f)
        {
            worldWidth = Mathf.Max(0.001f, areaWidth);
            worldHeight = Mathf.Max(0.001f, areaHeight);
            return;
        }

        float coverScale = Mathf.Max(areaWidth / spriteSize.x, areaHeight / spriteSize.y);
        worldWidth = spriteSize.x * coverScale;
        worldHeight = spriteSize.y * coverScale;
    }

    static Mesh BuildFlatMesh(float width, float height)
    {
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        var mesh = new Mesh { name = "BattleBackgroundMesh" };

        mesh.vertices = new[]
        {
            new Vector3(-halfW, -halfH, 0f),
            new Vector3(halfW, -halfH, 0f),
            new Vector3(-halfW, halfH, 0f),
            new Vector3(halfW, halfH, 0f),
        };

        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };

        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    void BuildCloudLayers(System.Func<string, Sprite> spriteResolver)
    {
        _cloudLayers.Clear();
        _cloudPrefabPoolIndex = -1;
        _cloudUsePool = false;

        if (_data.cloudLayers == null || _data.cloudLayers.Length == 0)
            return;

        if (!Application.isPlaying || !ShouldUseCloudPool())
        {
            Logger.Warn(
                "[BattleStageBackground] Cloud pool unavailable; cloud layers skipped.",
                LogTag.Pool);
            return;
        }

        string prefabId = string.IsNullOrEmpty(_data.cloudPrefabId)
            ? BattleStageCloudPoolable.DefaultPrefabId
            : _data.cloudPrefabId;

        _cloudPrefabPoolIndex = GameResDB.Instance.GetPrefabIndex(prefabId);
        if (_cloudPrefabPoolIndex < 0)
        {
            Logger.Warn(
                $"[BattleStageBackground] Cloud pool prefab '{prefabId}' not found; cloud layers skipped.",
                LogTag.Pool);
            return;
        }

        _cloudUsePool = true;

        for (int i = 0; i < _data.cloudLayers.Length; i++)
        {
            var cfg = _data.cloudLayers[i];
            if (cfg == null || string.IsNullOrEmpty(cfg.textureId))
                continue;

            var sprite = spriteResolver(cfg.textureId);
            if (sprite == null)
                continue;

            _cloudLayers.Add(new CloudLayerRuntime
            {
                config = cfg,
                sprite = sprite,
                spawnTimer = Random.Range(0f, cfg.spawnIntervalSeconds),
            });
        }
    }

    void SpawnCloud(CloudLayerRuntime layer, float topY)
    {
        if (!_cloudUsePool)
            return;

        float scale = Random.Range(layer.config.scaleRange.x, layer.config.scaleRange.y);
        float halfSpan = _area.Width * 0.5f;
        float x = Random.Range(-halfSpan, halfSpan);
        float y = topY + Random.Range(0f, _area.Height * 0.1f);
        float bottomY = _area.Bottom - _area.Height * 0.15f;
        float halfHeight = layer.sprite.bounds.extents.y * scale;

        var go = GameObjectPoolManager.Instance.Get(_cloudPrefabPoolIndex);
        if (go == null)
        {
            Logger.Error(
                $"[BattleStageBackground] Cloud pool exhausted (index={_cloudPrefabPoolIndex}). Increase warmup in GlobalPoolConfig.",
                LogTag.Pool);
            return;
        }

        go.transform.SetParent(cloudsRoot, false);
        BattleStageCloudPresentation.Apply(go, layer.sprite, layer.config, scale);
        go.transform.localPosition = new Vector3(x, y, 0f);
        go.SetActive(true);

        layer.instances.Add(new CloudInstance
        {
            gameObject = go,
            fallSpeed = layer.config.fallSpeed,
            recycleBottomY = bottomY,
            halfHeight = halfHeight,
        });
    }

    static void RecycleCloudInstance(CloudInstance inst)
    {
        if (inst.gameObject == null)
            return;

        var pool = GameObjectPoolManager.Instance;
        if (pool != null && pool.IsPoolActive)
            pool.Return(inst.gameObject);
        else
            DestroyGameObjectSafe(inst.gameObject);
    }

    void ReturnAllCloudInstances()
    {
        for (int li = 0; li < _cloudLayers.Count; li++)
        {
            var layer = _cloudLayers[li];
            for (int i = 0; i < layer.instances.Count; i++)
                RecycleCloudInstance(layer.instances[i]);
            layer.instances.Clear();
        }
    }

    void TryShake(ShakeProfile profile)
    {
        if (_shakeTarget == null)
            return;

        _activeShake?.Kill();
        _shakeTarget.localPosition = _restShakeLocalPosition;

        _activeShake = _shakeTarget
            .DOShakePosition(profile.Duration, profile.Strength, profile.Vibrato, fadeOut: true)
            .OnComplete(ResetShakeLocalPosition);
    }

    void ResetShakeLocalPosition()
    {
        if (_shakeTarget != null)
            _shakeTarget.localPosition = _restShakeLocalPosition;
    }

    void ClearVisualsInternal()
    {
        _activeShake?.Kill();
        _activeShake = null;
        _active = false;
        _simulationEnabled = false;

        ReturnAllCloudInstances();
        _cloudLayers.Clear();
        _cloudPrefabPoolIndex = -1;
        _cloudUsePool = false;

        for (int i = 0; i < _backgroundLayers.Count; i++)
            DestroyObjectSafe(_backgroundLayers[i].material);

        _backgroundLayers.Clear();
        DestroyObjectSafe(_sharedBackgroundMesh);
        _sharedBackgroundMesh = null;

        _shakeTarget = null;

        ClearChildren(backgroundRoot);
        ClearChildren(cloudsRoot);
    }

    static void ClearChildren(Transform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            DestroyGameObjectSafe(root.GetChild(i).gameObject);
    }

    static void DestroyObjectSafe(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    static void DestroyGameObjectSafe(GameObject go) => DestroyObjectSafe(go);

    void OnDestroy()
    {
        _activeShake?.Kill();
        _activeShake = null;

        for (int i = 0; i < _backgroundLayers.Count; i++)
            DestroyObjectSafe(_backgroundLayers[i].material);

        DestroyObjectSafe(_sharedBackgroundMesh);
    }
}
