using UnityEngine;

[CreateAssetMenu(fileName = "CollisionLayerMatrix", menuName = "Configs/Collision/CollisionLayerMatrix")]
public class CollisionLayerMatrixConfig : GameConfig
{
    [Tooltip("对称 L×L 矩阵：pairCollisions[row * LayerCount + col] 表示两层是否检测碰撞")]
    [SerializeField] bool[] pairCollisions = new bool[ColliderLayerDefinitions.LayerCount * ColliderLayerDefinitions.LayerCount];

    [Tooltip("勾选层的实体主动发起空间网格粗测；未勾选层仅作为被查询方")]
    [SerializeField] bool[] initiatesBroadphase = new bool[ColliderLayerDefinitions.LayerCount];

    public bool[] PairCollisions => pairCollisions;
    public bool[] InitiatesBroadphase => initiatesBroadphase;

    public void EnsureArraySizes()
    {
        int pairLen = ColliderLayerDefinitions.LayerCount * ColliderLayerDefinitions.LayerCount;
        if (pairCollisions == null || pairCollisions.Length != pairLen)
            pairCollisions = new bool[pairLen];

        if (initiatesBroadphase == null || initiatesBroadphase.Length != ColliderLayerDefinitions.LayerCount)
            initiatesBroadphase = new bool[ColliderLayerDefinitions.LayerCount];
    }

    public bool GetPair(int row, int col)
    {
        EnsureArraySizes();
        return pairCollisions[ColliderLayerDefinitions.PairIndex(row, col)];
    }

    public void SetPairSymmetric(int row, int col, bool value)
    {
        EnsureArraySizes();
        pairCollisions[ColliderLayerDefinitions.PairIndex(row, col)] = value;
        if (row != col)
            pairCollisions[ColliderLayerDefinitions.PairIndex(col, row)] = value;
    }

    public void ClearAllPairs(bool value)
    {
        EnsureArraySizes();
        for (int i = 0; i < pairCollisions.Length; i++)
            pairCollisions[i] = value;
    }

    public bool GetInitiatesBroadphase(E_ColliderLayer layer)
    {
        int idx = ColliderLayerDefinitions.ToIndex(layer);
        if (idx < 0)
            return false;

        EnsureArraySizes();
        return initiatesBroadphase[idx];
    }

    public void SetInitiatesBroadphase(E_ColliderLayer layer, bool value)
    {
        int idx = ColliderLayerDefinitions.ToIndex(layer);
        if (idx < 0)
            return;

        EnsureArraySizes();
        initiatesBroadphase[idx] = value;
    }

    public bool HasAnyCollisionEnabled()
    {
        EnsureArraySizes();
        for (int i = 0; i < pairCollisions.Length; i++)
        {
            if (pairCollisions[i])
                return true;
        }

        return false;
    }

    void OnValidate()
    {
        EnsureArraySizes();
    }
}
