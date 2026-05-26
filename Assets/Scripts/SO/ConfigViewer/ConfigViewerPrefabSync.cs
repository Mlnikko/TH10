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

        RunWhenEditorSafe(() => ApplyDanmakuEmitterDisplaySpriteImmediate(config));
    }

    static void ApplyDanmakuEmitterDisplaySpriteImmediate(DanmakuEmitterConfig config)
    {
        if (config == null)
            return;

        GameObject prefab = ConfigViewerAssetLookup.FindDanmakuEmitterPrefab(config.emitterPrefabId);
        if (prefab == null)
            return;

        DanmakuEmitterPresentation.Apply(config, prefab);
        MarkPrefabDirty(prefab);

        SyncAllViewersUsingConfig(config);
    }

    static void SyncAllViewersUsingConfig(DanmakuEmitterConfig config)
    {
        if (config == null)
            return;

#if UNITY_2023_1_OR_NEWER
        var viewers = Object.FindObjectsByType<DanmakuEmitterConfigViewer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var viewers = Object.FindObjectsOfType<DanmakuEmitterConfigViewer>(true);
#endif
        for (int i = 0; i < viewers.Length; i++)
        {
            var viewer = viewers[i];
            if (viewer == null || viewer.emitterConfig != config)
                continue;

            viewer.SyncDisplaySpriteFromConfig();
            EditorUtility.SetDirty(viewer);
        }
    }

    /// <summary>
    /// 避开 OnValidate / CheckConsistency 阶段修改 SpriteRenderer（会触发 SendMessage 报错）。
    /// </summary>
    static void RunWhenEditorSafe(System.Action action)
    {
        if (action == null)
            return;

        EditorApplication.delayCall += () => action();
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
