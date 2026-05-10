using UnityEngine;

/// <summary>
/// 挂载在掉落物预制体上：编辑 <see cref="DropItemConfig"/>、预览 Sprite 与碰撞体 Gizmo。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DropItemConfigViewer : MonoBehaviour
{
    [Header("配置文件")]
    public DropItemConfig dropItemConfig;

    [Header("表现")]
    [SerializeField] string pickupPrefabId;

    [SerializeField] Sprite pickupSprite;

    [Header("运动")]
    [SerializeField] float fallSpeed;

    [Header("碰撞")]
    [SerializeField] ColliderConfig colliderConfig;

    [Header("拾取效果")]
    [SerializeField] E_DropKind dropKind;

    [SerializeField] int effectAmount;

    public void LoadDropItemConfig()
    {
        if (dropItemConfig == null)
        {
            Logger.Warn("掉落物配置文件未设置", LogTag.Config);
            return;
        }

        pickupPrefabId = dropItemConfig.pickupPrefabId;
        pickupSprite = dropItemConfig.pickupSprite;
        fallSpeed = dropItemConfig.fallSpeed;
        colliderConfig = dropItemConfig.colliderConfig;
        dropKind = dropItemConfig.dropKind;
        effectAmount = dropItemConfig.effectAmount;

        Logger.Debug($"掉落物配置加载完成: {dropItemConfig.name}");
    }

    public void SaveDropItemConfig()
    {
        if (dropItemConfig == null)
        {
            Logger.Warn("掉落物配置文件未设置", LogTag.Config);
            return;
        }

        dropItemConfig.pickupPrefabId = pickupPrefabId;
        dropItemConfig.pickupSprite = pickupSprite;
        dropItemConfig.fallSpeed = fallSpeed;
        dropItemConfig.colliderConfig = colliderConfig;
        dropItemConfig.dropKind = dropKind;
        dropItemConfig.effectAmount = effectAmount;

#if UNITY_EDITOR
        dropItemConfig.BakeLogicTiming(GameManager.logicFPS > 0 ? GameManager.logicFPS : 60);
#endif

        Logger.Debug($"掉落物配置保存完成: {dropItemConfig.name}");
    }

    public void PreviewDropItem()
    {
        LoadDropItemConfig();

        if (TryGetComponent<SpriteRenderer>(out var spriteRenderer))
        {
            if (pickupSprite != null)
                spriteRenderer.sprite = pickupSprite;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (dropItemConfig == null)
            return;

        GizmosDrawer.ColliderDrawer(
            transform.position,
            transform.rotation,
            transform.localScale.x,
            colliderConfig,
            Color.cyan,
            Color.green);
    }
}
