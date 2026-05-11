using System;
using UnityEngine;

/// <summary>
/// 单条面板注册：脚本类名 ↔ Addressables 预制体 id（不含 prefab_ 前缀）及展示策略。
/// </summary>
[Serializable]
public class UIPanelRegistryEntry
{
    [Tooltip("与继承 UIPanel 的类名完全一致，例如 BattleUIPanel")]
    public string panelScriptTypeName;

    [Tooltip("Addressables 资源 id（不含 prefab_），例如 battlepanel")]
    public string prefabResourceId;

    [Tooltip("为 true 时 ClosePanel 会 Destroy；为 false 时仅隐藏并保留实例（下次 Show 无加载）")]
    public bool destroyInstanceWhenClosed = true;

    [Tooltip("为 true 时每次显示将该面板 RectTransform 置于父 Canvas 子级最前")]
    public bool exclusiveFullscreen;
}

/// <summary>
/// UI 面板注册表：由 <see cref="GameResourceManifest.uiPanelRegistry"/> 或 UIManager 序列化引用提供。
/// </summary>
[CreateAssetMenu(fileName = "UIPanelRegistry", menuName = "Configs/UI/UI Panel Registry")]
public class UIPanelRegistry : ScriptableObject
{
    [Header("编辑器扫描")]
    [Tooltip("Inspector 中「扫描预制体并填充」时递归查找的根目录（需在 Assets 下）。prefab 资源 id 与文件名一致（小写），对标 Addressables 的 prefab_{文件名小写}。")]
    public string panelPrefabScanFolder = "Assets/Prefabs/UI_Panel";

    public UIPanelRegistryEntry[] entries = Array.Empty<UIPanelRegistryEntry>();

#if UNITY_EDITOR
    void OnValidate()
    {
        if (entries == null)
            return;
        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            if (e == null)
                continue;
            if (!string.IsNullOrEmpty(e.panelScriptTypeName))
                e.panelScriptTypeName = e.panelScriptTypeName.Trim();
            if (!string.IsNullOrEmpty(e.prefabResourceId))
                e.prefabResourceId = StringHelper.NormalizeResourceId(e.prefabResourceId);
        }
    }
#endif
}
