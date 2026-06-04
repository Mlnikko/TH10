using System.Runtime.CompilerServices;

/// <summary>
/// 碰撞层过滤门面：读 <see cref="ColliderLayerMatrix"/>（数据来自 <see cref="CollisionLayerMatrixConfig"/>）。
/// </summary>
public static class ColliderLayerFilter
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanCollide(E_ColliderLayer layerA, E_ColliderLayer layerB) =>
        ColliderLayerMatrix.CanCollide(layerA, layerB);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanCollide(in CCollider a, in CCollider b) =>
        ColliderLayerMatrix.CanCollide(a.layer, b.layer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ShouldInitiateBroadphase(in CCollider col)
    {
        if (!col.isActive)
            return false;

        return ColliderLayerMatrix.ShouldInitiateBroadphase(col.layer);
    }
}
