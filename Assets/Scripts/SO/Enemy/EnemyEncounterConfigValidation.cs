#if UNITY_EDITOR
// 勿放入 Editor/ 子目录：Encounter Config（运行时程序集）的 OnValidate 需要引用本类型；
// 仅 Editor/ 下的脚本属于 Assembly-CSharp-Editor，运行时程序集无法链接（CS0103）。
using UnityEditor;
using UnityEngine;

/// <summary>Encounter 引用的 <see cref="EnemyConfig"/> 与 <see cref="EnemyType"/> 一致性检查（仅编辑器）。</summary>
public static class EnemyEncounterConfigValidation
{
    public static void WarnEnemyTypeMismatch(
        Object context,
        string enemyConfigId,
        EnemyType expectedType,
        string encounterLabel)
    {
        if (string.IsNullOrWhiteSpace(enemyConfigId))
            return;

        string id = enemyConfigId.ToLowerInvariantTrimmed();
        var enemy = ConfigViewerAssetLookup.FindConfig<EnemyConfig>(id, "Assets/Configs/Enemy");
        if (enemy == null)
            return;

        if (enemy.enemyType == expectedType)
            return;

        Debug.LogWarning(
            $"[{encounterLabel}] enemyConfigId '{id}' 的 enemyType 为 {enemy.enemyType}，" +
            $"期望 {expectedType}。请在中场/关底 Encounter 与 EnemyConfig 间保持一致。",
            context);
    }
}
#endif
