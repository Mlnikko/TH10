using System;
using System.Runtime.CompilerServices;

/// <summary>
/// 位集，用于高效存储和操作大量布尔值状态。
/// </summary>
public class BitSet
{
    static readonly byte[] _tzLookup = new byte[32]
    {
        0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
        31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9
    };

    readonly uint[] _data;
    readonly int _length;

    public BitSet(int length)
    {
        _length = length;
        int wordCount = (length + 31) / 32;
        _data = new uint[wordCount];
    }

    public void Set(int index, bool value)
    {
        if ((uint)index >= (uint)_length) return;
        int word = index >> 5;
        int bit = index & 31;
        if (value)
            _data[word] |= (1u << bit);
        else
            _data[word] &= ~(1u << bit);
    }

    public bool Get(int index)
    {
        if ((uint)index >= (uint)_length) return false;
        int word = index >> 5;
        int bit = index & 31;
        return (_data[word] & (1u << bit)) != 0;
    }

    /// <summary>
    /// 清空所有位（设为 false）
    /// </summary>
    public void ClearAll()
    {
        Array.Clear(_data, 0, _data.Length); // 高效清零，无 GC
    }

    /// <summary>
    /// 高效写入所有置位索引到 output，返回数量
    /// </summary>
    /// <param name="output"></param>
    /// <returns></returns>
    public int GetSetBits(Span<int> output)
    {
        int written = 0;
        int maxWords = (_length + 31) / 32;

        for (int wordIndex = 0; wordIndex < maxWords; wordIndex++)
        {
            uint word = _data[wordIndex];
            if (word == 0) continue;

            int baseIndex = wordIndex * 32;
            while (word != 0 && written < output.Length)
            {
                // 获取最低位 1 的位置
                int tz = TrailingZeroCount(word);
                int index = baseIndex + tz;
                if (index >= _length) break;

                output[written++] = index;
                word &= word - 1; // 清除最低位
            }
            if (written >= output.Length) break;
        }
        return written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int TrailingZeroCount(uint x)
    {
        if (x == 0) return 32;
        // De Bruijn 算法：O(1)，无循环
        return _tzLookup[((uint)((x & -(int)x) * 0x077CB531U)) >> 27];
    }
}

