/// <summary>
/// 安全实体句柄，含版本号防止 Use-After-Free。
/// Index: 0~65535 (16 bits), Version: 0~65535 (16 bits)
/// </summary>
public readonly struct Entity
{
    readonly int _packed; // 高16位=Version, 低16位=Index

    public static readonly Entity Null = new(0); // 约定：0 表示无效

    Entity(int packed) => _packed = packed;

    public bool IsNull => _packed == 0;

    internal int Index => _packed & 0xFFFF;

    internal ushort Version => (ushort)(_packed >> 16);

    internal static Entity FromIndexAndVersion(int index, ushort version)
    {
        return new Entity((version << 16) | index);
    }
}