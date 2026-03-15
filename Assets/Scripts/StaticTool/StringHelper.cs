public static class StringHelper
{
    /// <summary>
    /// 转换为小写并去除首尾空白字符，适用于配置ID等需要统一格式的字符串处理。
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ToLowerInvariantTrimmed(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return str.ToLowerInvariant().Trim();
    }
}
