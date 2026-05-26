/// <summary>
/// 武器池化预制体 archetype id（小写，与 <see cref="GameResourceManifest.weaponPrefabIds"/> 一致）。
/// 多条 <see cref="WeaponConfig"/> 共用；武器本体无 Sprite，发射器布局由 <see cref="WeaponRuntimeLayoutView"/> 按 Config 动态挂载。
/// </summary>
public static class WeaponPrefabArchetypes
{
    public const string Layout = "weapon_tpl_layout";
}
