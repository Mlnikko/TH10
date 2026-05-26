using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 道中波次刷怪坐标：生成点须在战斗区外、GO 回收边界的内扩区域内（与 <see cref="EnemyMovementSystem"/> 退场规则一致）。
/// </summary>
public static class EnemyWaveSpawnMath
{
    const float EdgeEpsilon = 0.02f;

    public static bool TryResolveQueueEntrySpawn(
        EnemyWaveConfig wave,
        in BattleAreaData area,
        int waveIndex,
        uint spawnFrame,
        int entryIndex,
        out Vector2 pos)
    {
        pos = default;
        wave.EnsureSpawnQueueMigrated();
        if (wave.spawnQueue == null || entryIndex < 0 || entryIndex >= wave.spawnQueue.Length)
            return false;

        var positions = ComputeSpawnPositions(wave, area, waveIndex, spawnFrame);
        if (positions.Count == 0)
            return false;

        var entry = wave.spawnQueue[entryIndex];
        int slot = entry.spawnSlotIndex >= 0 ? entry.spawnSlotIndex : entryIndex;
        pos = positions[Mathf.Clamp(slot, 0, positions.Count - 1)];
        return true;
    }

    public static List<Vector2> ComputeSpawnPositions(
        EnemyWaveConfig wave,
        in BattleAreaData area,
        int waveIndex,
        uint spawnFrame)
    {
        int spawnCount = wave.ResolveSpawnCount();
        var list = new List<Vector2>(Mathf.Max(1, spawnCount));
        float bandHeight = ExteriorBandHeight(area);
        float maxSpanX = ExteriorHorizontalSpan(area);
        float maxSpanY = Mathf.Max(0.05f, bandHeight);
        Vector2 anchor = TopExteriorSpawnAnchor(area, wave.spawnOffset);

        switch (wave.spawnPattern)
        {
            case SpawnPattern.BossCenter:
                list.Add(new Vector2(area.Center.x + wave.spawnOffset.x, anchor.y + wave.spawnOffset.y));
                ClampSpawnListToExteriorRecycle(area, list);
                return list;

            case SpawnPattern.Line:
            {
                int n = Mathf.Max(1, spawnCount);
                float span = Mathf.Min(Mathf.Max(0.01f, wave.spawnAreaSize.x), maxSpanX);
                for (int i = 0; i < n; i++)
                {
                    float t = n == 1 ? 0.5f : i / (float)(n - 1);
                    float x = anchor.x + Mathf.Lerp(-span * 0.5f, span * 0.5f, t);
                    list.Add(new Vector2(x, anchor.y));
                }
                ClampSpawnListToExteriorRecycle(area, list);
                return list;
            }

            case SpawnPattern.Grid:
            {
                int n = Mathf.Max(1, spawnCount);
                int cols = Mathf.CeilToInt(Mathf.Sqrt(n));
                int rows = Mathf.CeilToInt(n / (float)cols);
                float sx = Mathf.Min(Mathf.Max(0.01f, wave.spawnAreaSize.x), maxSpanX);
                float sy = Mathf.Min(Mathf.Max(0.01f, wave.spawnAreaSize.y), maxSpanY);
                int k = 0;
                for (int r = 0; r < rows && k < n; r++)
                {
                    for (int c = 0; c < cols && k < n; c++, k++)
                    {
                        float ux = cols == 1 ? 0.5f : c / (float)(cols - 1);
                        float uy = rows == 1 ? 0.5f : r / (float)(rows - 1);
                        float x = anchor.x + Mathf.Lerp(-sx * 0.5f, sx * 0.5f, ux);
                        float y = anchor.y + Mathf.Lerp(-sy * 0.5f, sy * 0.5f, uy);
                        list.Add(new Vector2(x, y));
                    }
                }
                ClampSpawnListToExteriorRecycle(area, list);
                return list;
            }

            case SpawnPattern.Circle:
            {
                int n = Mathf.Max(1, spawnCount);
                float maxRad = Mathf.Min(
                    maxSpanX * 0.48f,
                    maxSpanY * 0.48f,
                    ExteriorBandHeight(area) * 0.45f);
                float rad = Mathf.Min(Mathf.Max(0.01f, wave.spawnAreaSize.x * 0.5f), maxRad);
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)n;
                    float ang = t * Mathf.PI * 2f;
                    list.Add(new Vector2(anchor.x + Mathf.Cos(ang) * rad, anchor.y + Mathf.Sin(ang) * rad));
                }
                ClampSpawnListToExteriorRecycle(area, list);
                return list;
            }

            case SpawnPattern.Random:
            default:
            {
                float sx = Mathf.Min(Mathf.Max(bandHeight * 0.5f, wave.spawnAreaSize.x), maxSpanX);
                float sy = Mathf.Min(Mathf.Max(bandHeight * 0.5f, wave.spawnAreaSize.y), maxSpanY);
                int n = Mathf.Max(1, spawnCount);
                for (int i = 0; i < n; i++)
                {
                    float rx = Deterministic01(spawnFrame, waveIndex, i, 0);
                    float ry = Deterministic01(spawnFrame, waveIndex, i, 1);
                    list.Add(new Vector2(
                        anchor.x + (rx - 0.5f) * sx,
                        anchor.y + (ry - 0.5f) * sy));
                }
                ClampSpawnListToExteriorRecycle(area, list);
                return list;
            }
        }
    }

    /// <summary>上沿外侧刷怪锚点（战斗区顶边与回收顶边之间的带状区域）。</summary>
    public static Vector2 TopExteriorSpawnAnchor(in BattleAreaData area, Vector2 spawnOffset)
    {
        float bandH = ExteriorBandHeight(area);
        float y = area.Top + bandH * 0.5f;
        y = Mathf.Clamp(y + spawnOffset.y, area.Top + EdgeEpsilon, area.RecycleTop - EdgeEpsilon);
        float x = Mathf.Clamp(
            area.Center.x + spawnOffset.x,
            area.RecycleLeft + EdgeEpsilon,
            area.RecycleRight - EdgeEpsilon);
        return new Vector2(x, y);
    }

    public static Vector2 ClampPointToExteriorRecycle(in BattleAreaData area, Vector2 p)
    {
        p.x = Mathf.Clamp(p.x, area.RecycleLeft + EdgeEpsilon, area.RecycleRight - EdgeEpsilon);
        p.y = Mathf.Clamp(p.y, area.RecycleBottom + EdgeEpsilon, area.RecycleTop - EdgeEpsilon);

        if (!area.IsPointInBattleArea(p.x, p.y))
            return p;

        float dl = p.x - area.Left;
        float dr = area.Right - p.x;
        float db = p.y - area.Bottom;
        float dt = area.Top - p.y;
        float min = Mathf.Min(dl, dr, db, dt);
        if (Mathf.Approximately(min, dl))
            p.x = area.Left - EdgeEpsilon;
        else if (Mathf.Approximately(min, dr))
            p.x = area.Right + EdgeEpsilon;
        else if (Mathf.Approximately(min, db))
            p.y = area.Bottom - EdgeEpsilon;
        else
            p.y = area.Top + EdgeEpsilon;

        p.x = Mathf.Clamp(p.x, area.RecycleLeft + EdgeEpsilon, area.RecycleRight - EdgeEpsilon);
        p.y = Mathf.Clamp(p.y, area.RecycleBottom + EdgeEpsilon, area.RecycleTop - EdgeEpsilon);
        return p;
    }

    public static void ClampSpawnListToExteriorRecycle(in BattleAreaData area, List<Vector2> list)
    {
        for (int i = 0; i < list.Count; i++)
            list[i] = ClampPointToExteriorRecycle(area, list[i]);
    }

    static float ExteriorBandHeight(in BattleAreaData area) =>
        Mathf.Max(0.08f, area.GO_RecycleMargin.y);

    static float ExteriorHorizontalSpan(in BattleAreaData area)
    {
        float w = area.RecycleRight - area.RecycleLeft;
        return Mathf.Max(0.05f, w - EdgeEpsilon * 4f);
    }

    static float Deterministic01(uint frame, int waveIndex, int spawnIndex, int salt)
    {
        uint x = frame * 2246822519u
                 + (uint)waveIndex * 3266489917u
                 + (uint)spawnIndex * 668265263u
                 + (uint)salt * 374761393u;
        x ^= x >> 16;
        x *= 2654435761u;
        x ^= x >> 13;
        x *= 3266489917u;
        x ^= x >> 16;
        return (x & 0xffffffu) / (float)0xffffffu;
    }
}
