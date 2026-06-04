using System.Runtime.CompilerServices;

/// <summary>
/// <see cref="E_ColliderLayer"/> 与碰撞矩阵下标映射（单层单 bit，顺序固定）。
/// </summary>
public static class ColliderLayerDefinitions
{
    public const int LayerCount = 6;

    public static readonly E_ColliderLayer[] OrderedLayers =
    {
        E_ColliderLayer.Default,
        E_ColliderLayer.Player,
        E_ColliderLayer.Enemy,
        E_ColliderLayer.PlayerDanmaku,
        E_ColliderLayer.EnemyDanmaku,
        E_ColliderLayer.Item,
    };

    public static readonly string[] DisplayNames =
    {
        "Default",
        "Player",
        "Enemy",
        "PlayerDanmaku",
        "EnemyDanmaku",
        "Item",
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToIndex(E_ColliderLayer layer)
    {
        uint v = (uint)layer;
        if (v == 0 || (v & (v - 1)) != 0)
            return -1;

        int idx = 0;
        while ((v & 1) == 0)
        {
            v >>= 1;
            idx++;
        }

        return idx < LayerCount ? idx : -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static E_ColliderLayer FromIndex(int index) =>
        (uint)index < LayerCount ? OrderedLayers[index] : E_ColliderLayer.None;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int PairIndex(int row, int col) => row * LayerCount + col;
}
