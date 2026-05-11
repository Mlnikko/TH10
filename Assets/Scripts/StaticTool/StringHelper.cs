public static class StringHelper
{
    /// <summary>
    /// 逻辑资源 id：<see cref="NormalizeResourceId"/> 的扩展形态。<c>null</c> 仍返回 <c>null</c>。
    /// </summary>
    public static string ToLowerInvariantTrimmed(this string str)
    {
        if (str == null) return null;
        return NormalizeResourceId(str);
    }

    /// <summary>
    /// 逻辑资源 id：Trim + Invariant 小写；保留下划线。不含 prefab_/cfg_ 等 Addressables 类别前缀。
    /// </summary>
    public static string NormalizeResourceId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        return raw.Trim().ToLowerInvariant();
    }
}
