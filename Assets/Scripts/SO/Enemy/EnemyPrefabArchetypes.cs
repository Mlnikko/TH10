/// <summary>
/// 敌人池化预制体 archetype id（小写，与 <see cref="GameResourceManifest.enemyPrefabIds"/> 一致）。
/// 全部 <see cref="EnemyConfig"/> 共用；Sprite / Animator 由 Config 在出池时应用。
/// </summary>
public static class EnemyPrefabArchetypes
{
    public const string Unit = "enemy_tpl_unit";
}
