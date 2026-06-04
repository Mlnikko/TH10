using System.Runtime.CompilerServices;

/// <summary>
/// 运行时碰撞层矩阵（由 <see cref="CollisionLayerMatrixConfig"/> 烘焙，锁步只读）。
/// </summary>
public static class ColliderLayerMatrix
{
    static bool _initialized;
    static bool[] _pairCollide;
    static bool[] _initiatesBroadphase;

    public static bool IsInitialized => _initialized;

    public static void Reset()
    {
        _initialized = false;
        _pairCollide = null;
        _initiatesBroadphase = null;
    }

    public static void Apply(CollisionLayerMatrixConfig config)
    {
        if (config == null)
        {
            ApplyBuiltInDefaults();
            return;
        }

        config.EnsureArraySizes();
        if (!config.HasAnyCollisionEnabled())
            CollisionLayerMatrixDefaults.ApplyGameplayDefaults(config);

        int pairLen = ColliderLayerDefinitions.LayerCount * ColliderLayerDefinitions.LayerCount;
        _pairCollide = new bool[pairLen];
        _initiatesBroadphase = new bool[ColliderLayerDefinitions.LayerCount];

        for (int i = 0; i < pairLen; i++)
            _pairCollide[i] = config.PairCollisions[i];

        for (int i = 0; i < ColliderLayerDefinitions.LayerCount; i++)
            _initiatesBroadphase[i] = config.InitiatesBroadphase[i];

        _initialized = true;
    }

    public static void ApplyBuiltInDefaults()
    {
        int pairLen = ColliderLayerDefinitions.LayerCount * ColliderLayerDefinitions.LayerCount;
        _pairCollide = new bool[pairLen];
        _initiatesBroadphase = new bool[ColliderLayerDefinitions.LayerCount];
        CollisionLayerMatrixDefaults.BakeRuntimeArrays(_pairCollide, _initiatesBroadphase);
        _initialized = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanCollide(E_ColliderLayer layerA, E_ColliderLayer layerB)
    {
        if (!_initialized)
            return false;

        int ia = ColliderLayerDefinitions.ToIndex(layerA);
        int ib = ColliderLayerDefinitions.ToIndex(layerB);
        if (ia < 0 || ib < 0)
            return false;

        return _pairCollide[ColliderLayerDefinitions.PairIndex(ia, ib)];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldInitiateBroadphase(E_ColliderLayer layer)
    {
        if (!_initialized)
            return false;

        int idx = ColliderLayerDefinitions.ToIndex(layer);
        if (idx < 0)
            return false;

        return _initiatesBroadphase[idx];
    }
}
