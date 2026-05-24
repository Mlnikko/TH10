#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 将 Config 中的 Sprite 等表现字段同步到关联预制体组件（编辑器专用）。
/// </summary>
public static class ConfigViewerPrefabSync
{
    public static void ApplyDanmakuEmitterDisplaySprite(DanmakuEmitterConfig config)
    {
        if (config == null)
            return;

        GameObject prefab = ConfigViewerAssetLookup.FindDanmakuEmitterPrefab(config.emitterPrefabId);
        if (prefab == null)
            return;

        if (!TryApplySprite(prefab, config.displaySprite))
            return;

        MarkPrefabDirty(prefab);

        var viewer = prefab.GetComponent<DanmakuEmitterConfigViewer>();
        if (viewer != null && viewer.emitterConfig == config)
            viewer.SyncDisplaySpriteFromConfig();
    }

    public static bool TryApplySprite(GameObject root, Sprite sprite)
    {
        if (root == null)
            return false;

        if (!root.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
            spriteRenderer = root.GetComponentInChildren<SpriteRenderer>(true);

        if (spriteRenderer == null || spriteRenderer.sprite == sprite)
            return false;

        spriteRenderer.sprite = sprite;
        return true;
    }

    static void MarkPrefabDirty(GameObject prefab)
    {
        EditorUtility.SetDirty(prefab);
        PrefabUtility.RecordPrefabInstancePropertyModifications(prefab);
    }
}
#endif
