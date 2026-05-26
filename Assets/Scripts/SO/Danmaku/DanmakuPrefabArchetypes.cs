/// <summary>
/// 弹幕池化预制体 archetype id（小写，与 <see cref="GameResourceManifest.danmakuPrefabIds"/> 一致）。
/// 多条 <see cref="DanmakuConfig"/> 可共用同一 prefab；表现由 Config 的 sprite/color/scale 驱动。
/// </summary>
public static class DanmakuPrefabArchetypes
{
    public const string PlayerNeedle = "dm_tpl_player_needle";
    public const string PlayerOrb = "dm_tpl_player_orb";
    public const string EnemyBall = "dm_boll";
    public const string EnemyStar = "dm_star";
}
