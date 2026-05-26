using System.Text;
using UnityEngine;

/// <summary>
/// 为表现层生成/回收失败等日志提供实体上下文（配置 id、预制体名、位置等）。
/// </summary>
public static class EntityPresentationDiagnostics
{
    public static string FormatEntity(Entity entity)
    {
        if (entity.IsNull) return "Entity(null)";
        return $"Entity(index={entity.Index}, version={entity.Version})";
    }

    public static string FormatSpawnFailure(EntityManager em, Entity entity, string reason)
    {
        return $"{FormatEntity(entity)} | {DescribePresentationContext(em, entity.Index)} | {reason}";
    }

    public static string DescribePresentationContext(EntityManager em, int entityIndex)
    {
        var sb = new StringBuilder(192);
        Entity entity = em.GetEntity(entityIndex);
        if (entity.IsNull)
        {
            sb.Append("invalid entity handle");
            return sb.ToString();
        }

        if (em.HasComponent<CPosition>(entity))
        {
            ref var pos = ref em.GetComponent<CPosition>(entity);
            sb.Append($"pos=({pos.x:F2}, {pos.y:F2}) ");
        }

        if (em.HasComponent<CDropItem>(entity))
            AppendDropItem(sb, em, entityIndex);
        else if (em.HasComponent<CDanmaku>(entity))
            AppendDanmaku(sb, em, entityIndex);
        else if (em.HasComponent<CPlayer>(entity))
            AppendPlayer(sb, em, entityIndex);
        else if (em.HasComponent<CEnemy>(entity))
            AppendEnemy(sb, em, entityIndex);
        else
            sb.Append("kind=Unknown ");

        AppendComponentFlags(sb, em, entity);
        return sb.ToString().TrimEnd();
    }

    static void AppendDropItem(StringBuilder sb, EntityManager em, int entityIndex)
    {
        var drops = em.GetComponentSpan<CDropItem>();
        if ((uint)entityIndex >= (uint)drops.Length) return;

        ref var drop = ref drops[entityIndex];
        var cfg = GameResDB.Instance?.GetConfig<DropItemConfig>(drop.cfgIndex);
        sb.Append("kind=DropItem ");
        AppendConfig(sb, cfg, drop.cfgIndex);
        if (cfg != null)
            AppendPrefab(sb, cfg.pickupPrefabIndex, cfg.pickupPrefabId);
    }

    static void AppendDanmaku(StringBuilder sb, EntityManager em, int entityIndex)
    {
        var danmakus = em.GetComponentSpan<CDanmaku>();
        if ((uint)entityIndex >= (uint)danmakus.Length) return;

        ref var danmaku = ref danmakus[entityIndex];
        var cfg = GameResDB.Instance?.GetConfig<DanmakuConfig>(danmaku.cfgIndex);
        sb.Append("kind=Danmaku ");
        AppendConfig(sb, cfg, danmaku.cfgIndex);
        if (cfg != null)
            AppendPrefab(sb, cfg.danmakuPrefabIndex, cfg.danmakuPrefabId);
    }

    static void AppendPlayer(StringBuilder sb, EntityManager em, int entityIndex)
    {
        var players = em.GetComponentSpan<CPlayer>();
        if ((uint)entityIndex >= (uint)players.Length) return;

        ref var player = ref players[entityIndex];
        var cfg = GameResDB.Instance?.GetConfig<CharacterConfig>(player.characterCfgIndex);
        sb.Append($"kind=Player playerIndex={player.playerIndex} ");
        AppendConfig(sb, cfg, player.characterCfgIndex);
        if (cfg != null)
            AppendPrefab(sb, cfg.characterPrefabIndex, cfg.characterPrefabId);
    }

    static void AppendEnemy(StringBuilder sb, EntityManager em, int entityIndex)
    {
        var enemies = em.GetComponentSpan<CEnemy>();
        if ((uint)entityIndex >= (uint)enemies.Length) return;

        ref var enemy = ref enemies[entityIndex];
        var cfg = GameResDB.Instance?.GetConfig<EnemyConfig>(enemy.enemyCfgIndex);
        sb.Append("kind=Enemy ");
        AppendConfig(sb, cfg, enemy.enemyCfgIndex);
        if (cfg != null)
        {
            sb.Append($"enemyType={cfg.enemyType} ");
            AppendPrefab(sb, cfg.enemyPrefabIndex, EnemyPrefabArchetypes.Unit);
        }
    }

    static void AppendConfig(StringBuilder sb, GameConfig cfg, int cfgIndex)
    {
        if (cfg == null)
            sb.Append($"configIndex={cfgIndex}(not found) ");
        else
            sb.Append($"configId='{cfg.ConfigId}' configAsset='{cfg.name}' ");
    }

    static void AppendPrefab(StringBuilder sb, int prefabIndex, string prefabId)
    {
        sb.Append($"prefabId='{prefabId ?? string.Empty}' ");
        sb.Append(FormatPrefab(prefabIndex));
        sb.Append(' ');
    }

    public static string FormatPrefab(int prefabIndex)
    {
        if (prefabIndex < 0)
            return $"prefabIndex={prefabIndex}(invalid)";

        GameObject prefab = GameResDB.IsInitialized ? GameResDB.Instance.GetPrefab(prefabIndex) : null;
        string prefabName = prefab != null ? prefab.name : "(missing in GameResDB)";
        return $"prefabIndex={prefabIndex} prefabGO='{prefabName}'";
    }

    static void AppendComponentFlags(StringBuilder sb, EntityManager em, Entity entity)
    {
        sb.Append("components=[");
        bool first = true;
        AppendFlag<CPoolGetTag>(sb, ref first, em, entity);
        AppendFlag<CPoolRecycleTag>(sb, ref first, em, entity);
        AppendFlag<CGameObjectLink>(sb, ref first, em, entity);
        AppendFlag<CCollider>(sb, ref first, em, entity);
        AppendFlag<CHealth>(sb, ref first, em, entity);
        AppendFlag<CDanmakuEmitter>(sb, ref first, em, entity);
        AppendFlag<CEnemyPathMovement>(sb, ref first, em, entity);
        sb.Append(']');
    }

    static void AppendFlag<T>(StringBuilder sb, ref bool first, EntityManager em, Entity entity)
        where T : struct, IComponent
    {
        if (!em.HasComponent<T>(entity)) return;
        if (!first) sb.Append(',');
        first = false;
        sb.Append(typeof(T).Name);
    }
}
