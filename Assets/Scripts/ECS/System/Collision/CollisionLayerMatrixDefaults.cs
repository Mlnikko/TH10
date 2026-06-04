/// <summary>
/// 碰撞层矩阵默认玩法（与当前 STG 规则一致，可在 Inspector 一键恢复）。
/// </summary>
public static class CollisionLayerMatrixDefaults
{
    public static void ApplyGameplayDefaults(CollisionLayerMatrixConfig config)
    {
        if (config == null)
            return;

        config.EnsureArraySizes();
        config.ClearAllPairs(false);

        SetPair(config, E_ColliderLayer.Player, E_ColliderLayer.Enemy, true);
        SetPair(config, E_ColliderLayer.Player, E_ColliderLayer.EnemyDanmaku, true);
        SetPair(config, E_ColliderLayer.Player, E_ColliderLayer.Item, true);
        SetPair(config, E_ColliderLayer.Enemy, E_ColliderLayer.PlayerDanmaku, true);

        for (int i = 0; i < ColliderLayerDefinitions.LayerCount; i++)
            config.SetInitiatesBroadphase(ColliderLayerDefinitions.FromIndex(i), false);

        config.SetInitiatesBroadphase(E_ColliderLayer.Default, true);
        config.SetInitiatesBroadphase(E_ColliderLayer.Player, true);
        config.SetInitiatesBroadphase(E_ColliderLayer.Enemy, true);
        config.SetInitiatesBroadphase(E_ColliderLayer.Item, true);
    }

    static void SetPair(CollisionLayerMatrixConfig config, E_ColliderLayer a, E_ColliderLayer b, bool value) =>
        config.SetPairSymmetric(ColliderLayerDefinitions.ToIndex(a), ColliderLayerDefinitions.ToIndex(b), value);

    /// <summary>写入运行时缓冲（不依赖 ScriptableObject）。</summary>
    public static void BakeRuntimeArrays(bool[] pairCollide, bool[] initiatesBroadphase)
    {
        if (pairCollide == null || pairCollide.Length != ColliderLayerDefinitions.LayerCount * ColliderLayerDefinitions.LayerCount)
            return;
        if (initiatesBroadphase == null || initiatesBroadphase.Length != ColliderLayerDefinitions.LayerCount)
            return;

        for (int i = 0; i < pairCollide.Length; i++)
            pairCollide[i] = false;

        SetRuntimePair(pairCollide, E_ColliderLayer.Player, E_ColliderLayer.Enemy, true);
        SetRuntimePair(pairCollide, E_ColliderLayer.Player, E_ColliderLayer.EnemyDanmaku, true);
        SetRuntimePair(pairCollide, E_ColliderLayer.Player, E_ColliderLayer.Item, true);
        SetRuntimePair(pairCollide, E_ColliderLayer.Enemy, E_ColliderLayer.PlayerDanmaku, true);

        for (int i = 0; i < initiatesBroadphase.Length; i++)
            initiatesBroadphase[i] = false;

        initiatesBroadphase[ColliderLayerDefinitions.ToIndex(E_ColliderLayer.Default)] = true;
        initiatesBroadphase[ColliderLayerDefinitions.ToIndex(E_ColliderLayer.Player)] = true;
        initiatesBroadphase[ColliderLayerDefinitions.ToIndex(E_ColliderLayer.Enemy)] = true;
        initiatesBroadphase[ColliderLayerDefinitions.ToIndex(E_ColliderLayer.Item)] = true;
    }

    static void SetRuntimePair(bool[] pairCollide, E_ColliderLayer a, E_ColliderLayer b, bool value)
    {
        int ia = ColliderLayerDefinitions.ToIndex(a);
        int ib = ColliderLayerDefinitions.ToIndex(b);
        if (ia < 0 || ib < 0)
            return;

        pairCollide[ColliderLayerDefinitions.PairIndex(ia, ib)] = value;
        if (ia != ib)
            pairCollide[ColliderLayerDefinitions.PairIndex(ib, ia)] = value;
    }
}
