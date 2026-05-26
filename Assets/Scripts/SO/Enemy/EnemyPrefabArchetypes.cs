/// <summary>
/// 敌人池化预制体 archetype id（小写，与 <see cref="GameResourceManifest.enemyPrefabIds"/> 一致）。
/// 多条 <see cref="EnemyConfig"/> 共用；表现由 Config 的 sprite / Animator 等驱动。
/// </summary>
public static class EnemyPrefabArchetypes
{
    public const string Unit = "enemy_tpl_unit";
}
