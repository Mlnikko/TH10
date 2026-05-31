using System;
using UnityEngine;

/// <summary>战斗背景循环滚动参数（世界单位/秒，方向在运行时归一化）。</summary>
[Serializable]
public struct BattleAreaBackgroundScrollSettings
{
    [Tooltip("滚动方向（如 (0,-1) 表示贴图向下移动）；零向量表示不滚动")]
    public Vector2 direction;

    [Min(0f)]
    [Tooltip("滚动速度（世界单位/秒）")]
    public float speed;

    public static BattleAreaBackgroundScrollSettings Default =>
        new() { direction = new Vector2(0f, -1f), speed = 0.35f };

    public Vector2 NormalizedDirection
    {
        get
        {
            if (direction.sqrMagnitude < 0.0001f)
                return Vector2.zero;
            return direction.normalized;
        }
    }
}

/// <summary>单层滚动背景：贴图 + 独立滚动参数与排序。</summary>
[Serializable]
public class BattleAreaBackgroundScrollLayerData
{
    [StageBackgroundTextureId]
    [Tooltip("背景贴图资源 id（Manifest 纹理，如 stg1bg）")]
    public string textureId;

    public BattleAreaBackgroundScrollSettings scroll = BattleAreaBackgroundScrollSettings.Default;

    public int sortingOrder = -100;

    [Range(0f, 1f)]
    public float alpha = 1f;
}

/// <summary>单层云雾：自战斗区上方生成并向下方运动；Sprite 运行时按 textureId 动态替换。</summary>
[Serializable]
public class BattleAreaCloudLayerData
{
    [StageBackgroundTextureId]
    [Tooltip("云雾贴图资源 id（Manifest 纹理，如 stg1_cloud）")]
    public string textureId;

    [Min(0f)]
    [Tooltip("下落速度（世界单位/秒）")]
    public float fallSpeed = 0.6f;

    [Min(0.1f)]
    [Tooltip("平均生成间隔（秒）")]
    public float spawnIntervalSeconds = 2.5f;

    public Vector2 scaleRange = new(0.8f, 1.4f);

    [Range(0f, 1f)]
    public float alpha = 0.75f;

    public int sortingOrder = -80;

    [Min(1)]
    public int maxActiveCount = 6;
}

/// <summary>关卡背景与云雾表现配置（存于 <see cref="StageTimelineConfig"/>）。</summary>
[Serializable]
public class BattleAreaBackgroundData
{
    public bool enabled = true;

    [PoolPrefabId(E_PoolCategory.Stage)]
    [Tooltip("云雾池化预制体 id（/Prefabs/Stage/，默认 cloud）；各层共用，运行时替换 Sprite")]
    public string cloudPrefabId = BattleStageCloudPoolable.DefaultPrefabId;

    [Tooltip("滚动背景层（由远及近；每层独立贴图与滚动）")]
    public BattleAreaBackgroundScrollLayerData[] backgroundLayers =
    {
        new() { textureId = "stg1bg", scroll = BattleAreaBackgroundScrollSettings.Default, sortingOrder = -100 },
    };

    public BattleAreaCloudLayerData[] cloudLayers =
    {
        new() { textureId = "stg1_cloud", fallSpeed = 0.55f, spawnIntervalSeconds = 3f, alpha = 0.7f },
        new() { textureId = "stg1_cloud_2", fallSpeed = 0.85f, spawnIntervalSeconds = 2f, alpha = 0.55f, sortingOrder = -75 },
    };
}
