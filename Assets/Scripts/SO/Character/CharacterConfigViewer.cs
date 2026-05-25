using UnityEngine;

/// <summary>角色预制体配置编辑；不参与运行时逻辑。</summary>
public class CharacterConfigViewer : GameConfigViewerBase
{
    protected override bool HasAssignedConfig => characterConfig != null;

    public CharacterConfig CharacterConfig => characterConfig;

    [SerializeField] CharacterConfig characterConfig;

    [Header("信息配置")]
    [SerializeField] E_Character characterName;

    [TextArea(1, 5)]
    [SerializeField] string description;

    [Header("生命配置")]
    [SerializeField] int maxHealth;

    [Header("移速配置")]
    [SerializeField] float speed;
    [SerializeField] float slowSpeed;

    [Header("移动碰撞体配置")]
    [SerializeField] ColliderConfig moveColliderConfig;

    [Header("受击碰撞体配置")]
    [SerializeField] ColliderConfig hitColliderConfig;

    [Header("擦弹碰撞体配置")]
    [SerializeField] ColliderConfig grazeColliderConfig;

    [Header("可选武器")]
    [SerializeField] string[] weaponConfigIds = System.Array.Empty<string>();

#if UNITY_EDITOR
    [Header("武器预览")]
    [Tooltip("在角色预制体上挂接的武器 ConfigId，仅编辑器预览")]
    [SerializeField] string previewWeaponConfigId;

    GameObject _previewWeaponInstance;
#endif

    public void LoadCharacterConfig() => LoadFromConfig();

    public override void LoadFromConfig()
    {
        if (characterConfig == null)
            return;

        characterName = characterConfig.character;
        description = characterConfig.description;

        maxHealth = characterConfig.maxHealth;

        speed = characterConfig.moveSpeed;
        slowSpeed = characterConfig.moveSlowSpeed;

        moveColliderConfig = characterConfig.moveColliderConfig;
        hitColliderConfig = characterConfig.hitColliderConfig;
        grazeColliderConfig = characterConfig.grazeColliderConfig;
        weaponConfigIds = characterConfig.weaponConfigIds != null
            ? (string[])characterConfig.weaponConfigIds.Clone()
            : System.Array.Empty<string>();

#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(previewWeaponConfigId)
            && weaponConfigIds != null
            && weaponConfigIds.Length > 0)
        {
            previewWeaponConfigId = weaponConfigIds[0];
        }

        RefreshPreviewWeapon();
#endif
    }

    public void SaveCharacterConfig()
    {
        if (characterConfig == null) return;

        characterConfig.character = characterName;
        characterConfig.description = description;

        characterConfig.maxHealth = maxHealth;

        characterConfig.moveSpeed = speed;
        characterConfig.moveSlowSpeed = slowSpeed;

        characterConfig.moveColliderConfig = moveColliderConfig;
        characterConfig.hitColliderConfig = hitColliderConfig;
        characterConfig.grazeColliderConfig = grazeColliderConfig;
        characterConfig.weaponConfigIds = weaponConfigIds != null
            ? (string[])weaponConfigIds.Clone()
            : System.Array.Empty<string>();
    }

#if UNITY_EDITOR
    protected override void ApplyEditorPreview() => RefreshPreviewWeapon();

    protected override void StopEditorPreviews() => DestroyPreviewWeapon();

    public void RefreshPreviewWeapon()
    {
        DestroyPreviewWeapon();

        if (string.IsNullOrWhiteSpace(previewWeaponConfigId))
            return;

        var weaponCfg = ConfigViewerAssetLookup.FindWeaponConfig(previewWeaponConfigId);
        if (weaponCfg == null)
        {
            Logger.Warn(
                $"[CharacterConfigViewer] 未找到 WeaponConfig: '{previewWeaponConfigId}'",
                LogTag.Config);
            return;
        }

        string prefabId = string.IsNullOrWhiteSpace(weaponCfg.weaponPrefabId)
            ? weaponCfg.ConfigId
            : weaponCfg.weaponPrefabId;
        GameObject prefab = ConfigViewerAssetLookup.FindPrefab(prefabId, "Assets/Prefabs/Weapon");
        if (prefab == null)
        {
            Logger.Warn(
                $"[CharacterConfigViewer] 未找到武器预制体: '{prefabId}'",
                LogTag.Resource);
            return;
        }

        _previewWeaponInstance = UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject;
        if (_previewWeaponInstance == null)
            return;

        _previewWeaponInstance.transform.localPosition = Vector3.zero;
        _previewWeaponInstance.transform.localRotation = Quaternion.identity;
        _previewWeaponInstance.SetActive(true);
    }

    void DestroyPreviewWeapon()
    {
        if (_previewWeaponInstance == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(_previewWeaponInstance);
        else
            Object.DestroyImmediate(_previewWeaponInstance);

        _previewWeaponInstance = null;
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
            RefreshPreviewWeapon();
    }
    void OnDrawGizmosSelected()
    {
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, moveColliderConfig, Color.cyan, Color.cyan);
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, hitColliderConfig, Color.red, Color.red);
        GizmosDrawer.ColliderDrawer(transform.position, transform.rotation, transform.localScale.x, grazeColliderConfig, Color.blue, Color.blue);
    }
#endif
}
