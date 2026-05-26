using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>敌人死亡掉落条目：掉落物种类 + 数量。</summary>
[Serializable]
public struct DeathDropEntry
{
    [Tooltip("DropItemConfig 的 ConfigId（小写）")]
    public string dropConfigId;

    [Min(1)]
    [Tooltip("生成该掉落物的实体数量")]
    public int count;
}

/// <summary>烘焙后的死亡掉落（配置索引 + 数量）。</summary>
public struct BakedDeathDropEntry
{
    public int cfgIndex;
    public int count;

    public BakedDeathDropEntry(int cfgIndex, int count)
    {
        this.cfgIndex = cfgIndex;
        this.count = Mathf.Max(1, count);
    }
}

/// <summary>死亡掉落条目解析与合并。</summary>
public static class DeathDropBaking
{
    public static BakedDeathDropEntry[] BakeEntries(DeathDropEntry[] entries, GameResDB resDb, string logContext)
    {
        if (entries == null || entries.Length == 0)
            return Array.Empty<BakedDeathDropEntry>();

        var list = new List<BakedDeathDropEntry>(entries.Length);
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (string.IsNullOrWhiteSpace(e.dropConfigId))
                continue;

            string id = e.dropConfigId.ToLowerInvariantTrimmed();
            int idx = resDb.GetConfigIndex(id);
            if (idx < 0)
            {
                Logger.Warn($"[{logContext}] DropItemConfig not found: '{id}'", LogTag.Resource);
                continue;
            }

            list.Add(new BakedDeathDropEntry(idx, e.count <= 0 ? 1 : e.count));
        }

        return list.ToArray();
    }

    public static int CountSpawnInstances(BakedDeathDropEntry[] entries)
    {
        if (entries == null || entries.Length == 0)
            return 0;
        int total = 0;
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].cfgIndex >= 0)
                total += Mathf.Max(1, entries[i].count);
        }

        return total;
    }

    public static BakedDeathDropEntry[] MergeAppend(BakedDeathDropEntry[] first, BakedDeathDropEntry[] second)
    {
        int lenA = first?.Length ?? 0;
        int lenB = second?.Length ?? 0;
        if (lenA == 0)
            return second ?? Array.Empty<BakedDeathDropEntry>();
        if (lenB == 0)
            return first ?? Array.Empty<BakedDeathDropEntry>();

        var merged = new BakedDeathDropEntry[lenA + lenB];
        for (int i = 0; i < lenA; i++)
            merged[i] = first[i];
        for (int i = 0; i < lenB; i++)
            merged[lenA + i] = second[i];
        return merged;
    }

#if UNITY_EDITOR
    public static void MigrateLegacyDropIds(ref DeathDropEntry[] entries, string[] legacyIds)
    {
        if (entries != null && entries.Length > 0)
            return;
        if (legacyIds == null || legacyIds.Length == 0)
            return;

        entries = new DeathDropEntry[legacyIds.Length];
        for (int i = 0; i < legacyIds.Length; i++)
        {
            entries[i] = new DeathDropEntry
            {
                dropConfigId = legacyIds[i],
                count = 1,
            };
        }
    }

    public static void NormalizeEntries(DeathDropEntry[] entries)
    {
        if (entries == null)
            return;
        for (int i = 0; i < entries.Length; i++)
        {
            if (!string.IsNullOrEmpty(entries[i].dropConfigId))
                entries[i].dropConfigId = entries[i].dropConfigId.ToLowerInvariantTrimmed();
            if (entries[i].count < 1)
                entries[i].count = 1;
        }
    }
#endif
}
