/// <summary>
/// 掉落物池化预制体 archetype id（小写，与 <see cref="GameResourceManifest.dropItemPrefabIds"/> 一致）。
/// 多条 <see cref="DropItemConfig"/> 共用同一 prefab；表现由 Config 的 <see cref="DropItemConfig.pickupSprite"/> 驱动。
/// </summary>
public static class DropItemPrefabArchetypes
{
    public const string Pickup = "drop_tpl_pickup";
}
