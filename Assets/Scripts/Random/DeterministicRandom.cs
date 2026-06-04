/// <summary>
/// 锁步逻辑用确定性伪随机：指定 seed 后跨平台、跨客户端结果一致。
/// 逻辑层请使用本模块，勿用 <see cref="UnityEngine.Random"/>。
/// </summary>
public struct DeterministicRandom
{
    uint _state;

    const uint NullSeedSubstitute = 0xA341316Cu;

    public DeterministicRandom(uint seed)
    {
        _state = seed == 0 ? NullSeedSubstitute : seed;
    }

    /// <summary>当前内部状态（随 <see cref="NextUInt"/> 推进）。</summary>
    public uint State => _state;

    public void Reseed(uint seed)
    {
        _state = seed == 0 ? NullSeedSubstitute : seed;
    }

    /// <summary>返回 [0, uint.MaxValue]。</summary>
    public uint NextUInt()
    {
        uint z = _state += 0x9E3779B9u;
        z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
        z = (z ^ (z >> 13)) * 0xC2B2AE35u;
        return z ^ (z >> 16);
    }

    /// <summary>返回完整 32 位有符号整数（各值概率均等）。</summary>
    public int NextInt() => (int)NextUInt();

    /// <summary>返回 [minInclusive, maxExclusive)。</summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        uint range = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)(NextUInt() % range);
    }

    /// <summary>返回 [0, 1)。</summary>
    public float NextFloat01() => (NextUInt() >> 8) * (1f / 16777216f);
}

/// <summary>
/// 无状态确定性哈希采样：相同 seed 与 salt 组合恒得到相同结果，适合按索引取随机值（粒弹、刷怪点等）。
/// </summary>
public static class DeterministicRandomHash
{
    internal const uint Salt0Mul = 3266489917u;
    internal const uint Salt1Mul = 668265263u;
    internal const uint Salt2Mul = 374761393u;

    /// <summary>无状态哈希，相同 seed 与 salt 组合恒得到相同值。</summary>
    public static uint ToUInt(uint seed, int salt0 = 0, int salt1 = 0, int salt2 = 0)
    {
        uint x = seed
                 + (uint)salt0 * Salt0Mul
                 + (uint)salt1 * Salt1Mul
                 + (uint)salt2 * Salt2Mul;
        return FinalMix(x);
    }

    /// <summary>返回 [minInclusive, maxExclusive)。</summary>
    public static int ToInt(uint seed, int minInclusive, int maxExclusive, int salt0 = 0, int salt1 = 0, int salt2 = 0)
    {
        if (maxExclusive <= minInclusive)
            return minInclusive;

        uint range = (uint)(maxExclusive - minInclusive);
        return minInclusive + (int)(ToUInt(seed, salt0, salt1, salt2) % range);
    }

    /// <summary>返回 [0, 1)。</summary>
    public static float ToFloat01(uint seed, int salt0 = 0, int salt1 = 0, int salt2 = 0)
        => (ToUInt(seed, salt0, salt1, salt2) & 0xffffffu) / (float)0x1000000u;

    /// <summary>由实体索引派生子 seed（与发射器/波次逻辑共用常量）。</summary>
    public static uint FromEntityIndex(int entityIndex, int salt = 0)
        => (uint)(entityIndex + 1) * Salt0Mul + (uint)(salt + 1) * Salt1Mul;

    static uint FinalMix(uint x)
    {
        x ^= x >> 16;
        x *= 2246822519u;
        x ^= x >> 13;
        x *= Salt0Mul;
        x ^= x >> 16;
        return x;
    }
}
