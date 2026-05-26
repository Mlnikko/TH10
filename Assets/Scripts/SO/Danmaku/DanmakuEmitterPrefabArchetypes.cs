/// <summary>
/// 弹幕发射器池化预制体 archetype id（小写，与 <see cref="GameResourceManifest.danmakuEmitterPrefabIds"/> 一致）。
/// 多条 <see cref="DanmakuEmitterConfig"/> 共用；表现由 <see cref="DanmakuEmitterConfig.displaySprite"/> 驱动。
/// </summary>
public static class DanmakuEmitterPrefabArchetypes
{
    public const string Sprite = "dme_tpl_sprite";
}
