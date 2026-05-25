#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 从 <see cref="GameResourceManifest"/>（或资源目录扫描）生成 ConfigId / PrefabId 下拉，避免手填字符串。
/// </summary>
public static class ResourceIdEditorPicker
{
    const string ManifestAssetPath = "Assets/Configs/GameResourceManifest.asset";

    public static void DrawPrefabIdField(SerializedProperty stringProp, string manifestArrayFieldName, string scanFolderUnderAssets)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectPrefabIds(manifestArrayFieldName, scanFolderUnderAssets);
        DrawIdPopup(stringProp, ids, "预制体 Id");
    }

    public static void DrawDanmakuConfigIdField(SerializedProperty stringProp)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectDanmakuConfigIds();
        DrawIdPopup(stringProp, ids, "弹幕 Config Id");
    }

    public static void DrawWeaponConfigIdField(SerializedProperty stringProp)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectWeaponConfigIds();
        DrawIdPopup(stringProp, ids, "武器 Config Id");
    }

    public static void DrawWeaponConfigIdAtRect(Rect rect, SerializedProperty stringProp, GUIContent label)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectWeaponConfigIds();
        DrawIdPopupAtRect(rect, stringProp, ids, label ?? GUIContent.none);
    }

    public static void DrawEnemyConfigIdField(SerializedProperty stringProp)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectEnemyConfigIds();
        DrawIdPopup(stringProp, ids, "敌人 Config Id");
    }

    public static void DrawDropItemConfigIdField(SerializedProperty stringProp)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectDropItemConfigIds();
        DrawIdPopup(stringProp, ids, "掉落 Config Id");
    }

    public static void DrawDropItemConfigIdAtRect(Rect rect, SerializedProperty stringProp, GUIContent label)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectDropItemConfigIds();
        DrawIdPopupAtRect(rect, stringProp, ids, label ?? GUIContent.none);
    }

    public static void DrawWeaponConfigIdArray(SerializedProperty arrayProp)
    {
        if (arrayProp == null || !arrayProp.isArray)
            return;

        EditorGUILayout.LabelField("可选武器", EditorStyles.boldLabel);

        var ids = CollectWeaponConfigIds();

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            var element = arrayProp.GetArrayElementAtIndex(i);
            DrawIdPopup(element, ids, $"武器 [{i}]");
            if (GUILayout.Button("−", GUILayout.Width(22)))
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ 添加武器", GUILayout.Width(100)))
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
    }

    public static void DrawDanmakuEmitterConfigIdField(SerializedProperty stringProp)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectDanmakuEmitterConfigIds();
        DrawIdPopup(stringProp, ids, "发射器 Config Id");
    }

    public static void DrawDanmakuEmitterConfigIdAtRect(Rect rect, SerializedProperty stringProp, GUIContent label)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectDanmakuEmitterConfigIds();
        DrawIdPopupAtRect(rect, stringProp, ids, label ?? GUIContent.none);
    }

    public static IReadOnlyList<string> GetDanmakuEmitterConfigIds() => CollectDanmakuEmitterConfigIds();

    public static void DrawPoolPrefabIdField(SerializedProperty stringProp, E_PoolCategory category)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectPoolPrefabIds(category);
        DrawIdPopup(stringProp, ids, "预制体 Id");
    }

    public static void DrawDropItemPrefabIdField(SerializedProperty stringProp) =>
        DrawPoolPrefabIdField(stringProp, E_PoolCategory.Drop);

    public static void DrawPoolPrefabIdAtRect(Rect rect, SerializedProperty stringProp, GUIContent label, E_PoolCategory category)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        var ids = CollectPoolPrefabIds(category);
        DrawIdPopupAtRect(rect, stringProp, ids, label ?? GUIContent.none);
    }

    public static E_PoolCategory ResolvePoolCategoryFromProperty(SerializedProperty prefabIdProp)
    {
        if (prefabIdProp == null)
            return E_PoolCategory.Other;

        SerializedProperty groupProp = FindPoolCategoryGroupProperty(prefabIdProp);
        if (groupProp == null)
            return E_PoolCategory.Other;

        SerializedProperty categoryProp = groupProp.FindPropertyRelative(nameof(PoolCategoryGroup.category));
        if (categoryProp == null || categoryProp.propertyType != SerializedPropertyType.Enum)
            return E_PoolCategory.Other;

        return (E_PoolCategory)categoryProp.enumValueIndex;
    }

    static SerializedProperty FindPoolCategoryGroupProperty(SerializedProperty prefabIdProp)
    {
        string path = prefabIdProp.propertyPath;
        const string entriesMarker = ".entries.Array.data[";
        int idx = path.IndexOf(entriesMarker, StringComparison.Ordinal);
        if (idx < 0)
            return null;

        string groupPath = path.Substring(0, idx);
        return prefabIdProp.serializedObject.FindProperty(groupPath);
    }

    static List<string> CollectPoolPrefabIds(E_PoolCategory category)
    {
        switch (category)
        {
            case E_PoolCategory.Player:
                return CollectPrefabIds(
                    nameof(GameResourceManifest.characterPrefabIds),
                    "Prefabs/Character");
            case E_PoolCategory.Enemy:
                return CollectPrefabIds(
                    nameof(GameResourceManifest.enemyPrefabIds),
                    "Prefabs/Enemy");
            case E_PoolCategory.Danmaku:
                return CollectPrefabIds(
                    nameof(GameResourceManifest.danmakuPrefabIds),
                    "Prefabs/Danmaku");
            case E_PoolCategory.Drop:
                return CollectPrefabIds(
                    nameof(GameResourceManifest.dropItemPrefabIds),
                    "Prefabs/DropItem");
            case E_PoolCategory.Effect:
                return CollectPrefabIds(
                    nameof(GameResourceManifest.effectPrefabIds),
                    "Prefabs/Effect");
            case E_PoolCategory.Weapon:
                return CollectPrefabIds(
                    nameof(GameResourceManifest.weaponPrefabIds),
                    "Prefabs/Weapon");
            case E_PoolCategory.DanmakuEmitter:
                return CollectPrefabIds(
                    nameof(GameResourceManifest.danmakuEmitterPrefabIds),
                    "Prefabs/DanmakuEmitter");
            default:
                return CollectAllPoolPrefabIds();
        }
    }

    static List<string> CollectAllPoolPrefabIds()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddManifestIds(set, nameof(GameResourceManifest.characterPrefabIds));
        AddManifestIds(set, nameof(GameResourceManifest.weaponPrefabIds));
        AddManifestIds(set, nameof(GameResourceManifest.enemyPrefabIds));
        AddManifestIds(set, nameof(GameResourceManifest.danmakuPrefabIds));
        AddManifestIds(set, nameof(GameResourceManifest.danmakuEmitterPrefabIds));
        AddManifestIds(set, nameof(GameResourceManifest.dropItemPrefabIds));
        AddManifestIds(set, nameof(GameResourceManifest.effectPrefabIds));
        AddPrefabIdsFromFolder(set, "Prefabs/Character");
        AddPrefabIdsFromFolder(set, "Prefabs/Weapon");
        AddPrefabIdsFromFolder(set, "Prefabs/Enemy");
        AddPrefabIdsFromFolder(set, "Prefabs/Danmaku");
        AddPrefabIdsFromFolder(set, "Prefabs/DanmakuEmitter");
        AddPrefabIdsFromFolder(set, "Prefabs/DropItem");
        AddPrefabIdsFromFolder(set, "Prefabs/Effect");
        return SortIds(set);
    }

    const float WeaponRowRemoveButtonWidth = 24f;

    public static void DrawWeaponPowerPrimarySlowLayouts(SerializedProperty arrayProp)
    {
        if (arrayProp == null || !arrayProp.isArray)
            return;

        EditorGUILayout.LabelField("低速主炮（按 Power）", EditorStyles.boldLabel);
        var emitterIds = CollectDanmakuEmitterConfigIds();

        for (int t = 0; t < arrayProp.arraySize; t++)
        {
            var tier = arrayProp.GetArrayElementAtIndex(t);
            var minPower = tier.FindPropertyRelative(nameof(WeaponPowerPrimarySlowLayout.minPowerOrbs));
            var slot = tier.FindPropertyRelative(nameof(WeaponPowerPrimarySlowLayout.slot));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (DrawWeaponPowerTierHeader(minPower, $"档位 [{t}]", arrayProp, t))
                break;

            DrawWeaponEmitterSlot(slot, emitterIds, "主炮槽位");
            EditorGUILayout.EndVertical();
        }

        DrawWeaponArrayAddButton("+ 添加主炮档位", 110, () => arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize));
    }

    public static void DrawWeaponPowerSecondaryLayouts(SerializedProperty arrayProp)
    {
        if (arrayProp == null || !arrayProp.isArray)
            return;

        EditorGUILayout.LabelField("副炮（按 Power）", EditorStyles.boldLabel);
        var emitterIds = CollectDanmakuEmitterConfigIds();

        for (int t = 0; t < arrayProp.arraySize; t++)
        {
            var tier = arrayProp.GetArrayElementAtIndex(t);
            var minPower = tier.FindPropertyRelative(nameof(WeaponPowerSecondaryLayout.minPowerOrbs));
            var slots = tier.FindPropertyRelative(nameof(WeaponPowerSecondaryLayout.slots));

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (DrawWeaponPowerTierHeader(minPower, $"档位 [{t}]", arrayProp, t))
                break;

            DrawWeaponSecondarySlotsArray(slots, emitterIds);
            EditorGUILayout.EndVertical();
        }

        DrawWeaponArrayAddButton("+ 添加副炮档位", 110, () => arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize));
    }

    static bool DrawWeaponPowerTierHeader(
        SerializedProperty minPowerProp,
        string tierLabel,
        SerializedProperty tierArrayProp,
        int tierIndex)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(minPowerProp, new GUIContent($"最低 Power (P≥) · {tierLabel}"));
        bool removed = GUILayout.Button(new GUIContent("−", "删除本 Power 档位"), GUILayout.Width(WeaponRowRemoveButtonWidth));
        EditorGUILayout.EndHorizontal();

        if (removed)
        {
            tierArrayProp.DeleteArrayElementAtIndex(tierIndex);
            return true;
        }

        return false;
    }

    static void DrawWeaponSecondarySlotsArray(SerializedProperty slotsProp, IReadOnlyList<string> emitterIds)
    {
        if (slotsProp == null || !slotsProp.isArray)
            return;

        EditorGUILayout.LabelField("副炮槽位", EditorStyles.miniBoldLabel);

        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float blockHeight = line * 2f + spacing;

        for (int i = 0; i < slotsProp.arraySize; i++)
        {
            var slot = slotsProp.GetArrayElementAtIndex(i);
            var configIdProp = slot.FindPropertyRelative(nameof(WeaponEmitterSlot.danmakuEmitterConfigId));
            var offsetProp = slot.FindPropertyRelative(nameof(WeaponEmitterSlot.slotOffset));
            if (configIdProp == null || offsetProp == null)
                continue;

            Rect block = EditorGUILayout.GetControlRect(false, blockHeight);
            float y = block.y;

            Rect removeRect = new Rect(block.xMax - WeaponRowRemoveButtonWidth, y, WeaponRowRemoveButtonWidth, line);
            Rect popupRect = new Rect(block.x, y, block.width - WeaponRowRemoveButtonWidth - 2f, line);
            DrawIdPopupAtRect(popupRect, configIdProp, emitterIds, new GUIContent($"副炮 [{i}]"));

            if (GUI.Button(removeRect, new GUIContent("−", "移除此副炮槽位")))
            {
                slotsProp.DeleteArrayElementAtIndex(i);
                break;
            }

            y += line + spacing;
            EditorGUI.PropertyField(
                new Rect(block.x, y, block.width, line),
                offsetProp,
                new GUIContent("槽位偏移"));

            EditorGUILayout.Space(2);
        }

        DrawWeaponArrayAddButton("+ 添加副炮槽", 100, () => slotsProp.InsertArrayElementAtIndex(slotsProp.arraySize));
    }

    public static void DrawWeaponEmitterSlot(SerializedProperty slotProp, IReadOnlyList<string> emitterIds, string label)
    {
        if (slotProp == null)
            return;

        if (!string.IsNullOrEmpty(label))
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

        DrawWeaponEmitterSlotFields(slotProp, emitterIds);
    }

    static void DrawWeaponEmitterSlotFields(SerializedProperty slotProp, IReadOnlyList<string> emitterIds)
    {
        var configIdProp = slotProp.FindPropertyRelative(nameof(WeaponEmitterSlot.danmakuEmitterConfigId));
        var offsetProp = slotProp.FindPropertyRelative(nameof(WeaponEmitterSlot.slotOffset));
        if (configIdProp == null || offsetProp == null)
            return;

        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        Rect block = EditorGUILayout.GetControlRect(false, line * 2f + spacing);
        float y = block.y;

        DrawIdPopupAtRect(
            new Rect(block.x, y, block.width, line),
            configIdProp,
            emitterIds,
            new GUIContent("发射器", "选 (无) 可清除发射器配置"));

        y += line + spacing;
        EditorGUI.PropertyField(
            new Rect(block.x, y, block.width, line),
            offsetProp,
            new GUIContent("槽位偏移"));
    }

    static void DrawWeaponArrayAddButton(string label, float width, System.Action onAdd)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(label, GUILayout.Width(width)))
            onAdd();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    public static float GetDropItemConfigIdArrayHeight(SerializedProperty arrayProp)
    {
        if (arrayProp == null || !arrayProp.isArray)
            return EditorGUIUtility.singleLineHeight;

        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        int rowCount = Mathf.Max(0, arrayProp.arraySize);
        return rowCount * (line + spacing) + line + spacing * 2f;
    }

    public static void DrawDropItemConfigIdArray(SerializedProperty arrayProp)
    {
        DrawDropItemConfigIdArray(arrayProp, drawSectionHeader: true);
    }

    public static void DrawDropItemConfigIdArrayAtRect(Rect position, SerializedProperty arrayProp, GUIContent label)
    {
        if (arrayProp == null || !arrayProp.isArray)
            return;

        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        float y = position.y;
        float width = position.width;

        if (label != null && !string.IsNullOrEmpty(label.text))
        {
            EditorGUI.LabelField(new Rect(position.x, y, width, line), label.text, EditorStyles.boldLabel);
            y += line + spacing;
        }

        y = DrawDropItemConfigIdArrayRows(
            new Rect(position.x, y, width, position.yMax - y),
            arrayProp,
            y);

        Rect addButtonRect = new Rect(position.x + width - 100f, y, 100f, line);
        if (GUI.Button(addButtonRect, "+ 添加掉落"))
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
    }

    public static void DrawDropItemConfigIdArray(SerializedProperty arrayProp, bool drawSectionHeader)
    {
        if (arrayProp == null || !arrayProp.isArray)
            return;

        if (drawSectionHeader)
            EditorGUILayout.LabelField("死亡掉落", EditorStyles.boldLabel);

        var ids = CollectDropItemConfigIds();
        DrawDropItemConfigIdArrayRowsGUILayout(arrayProp, ids);
        EditorGUILayout.Space(2);
    }

    static void DrawDropItemConfigIdArrayRowsGUILayout(SerializedProperty arrayProp, IReadOnlyList<string> ids)
    {
        const float removeButtonWidth = 24f;

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            var element = arrayProp.GetArrayElementAtIndex(i);
            DrawIdPopup(element, ids, $"掉落 [{i}]");
            if (GUILayout.Button("−", GUILayout.Width(removeButtonWidth)))
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ 添加掉落", GUILayout.Width(100)))
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
        EditorGUILayout.EndHorizontal();
    }

    static float DrawDropItemConfigIdArrayRows(Rect position, SerializedProperty arrayProp, float startY)
    {
        if (arrayProp == null || !arrayProp.isArray)
            return startY;

        var ids = CollectDropItemConfigIds();
        float line = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        const float removeButtonWidth = 24f;
        float y = startY;

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            var element = arrayProp.GetArrayElementAtIndex(i);
            Rect rowRect = new Rect(position.x, y, position.width, line);
            Rect removeRect = new Rect(rowRect.xMax - removeButtonWidth, rowRect.y, removeButtonWidth, rowRect.height);
            Rect popupRect = new Rect(rowRect.x, rowRect.y, rowRect.width - removeButtonWidth - 2f, rowRect.height);

            DrawIdPopupAtRect(popupRect, element, ids, new GUIContent($"掉落 [{i}]"));
            if (GUI.Button(removeRect, "−"))
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                break;
            }

            y += line + spacing;
        }

        return y;
    }

    public static void DrawDanmakuConfigIdArray(SerializedProperty arrayProp)
    {
        if (arrayProp == null || !arrayProp.isArray)
            return;

        EditorGUILayout.LabelField("装填弹幕配置", EditorStyles.boldLabel);

        var ids = CollectDanmakuConfigIds();

        for (int i = 0; i < arrayProp.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            var element = arrayProp.GetArrayElementAtIndex(i);
            DrawIdPopup(element, ids, $"弹幕 [{i}]");
            if (GUILayout.Button("−", GUILayout.Width(22)))
            {
                arrayProp.DeleteArrayElementAtIndex(i);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ 添加弹幕", GUILayout.Width(100)))
            arrayProp.InsertArrayElementAtIndex(arrayProp.arraySize);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
    }

    static void DrawIdPopup(SerializedProperty stringProp, IReadOnlyList<string> registeredIds, string label)
    {
        if (stringProp == null)
            return;

        string current = stringProp.stringValue ?? string.Empty;
        BuildPopupOptions(current, registeredIds, out string[] labels, out string[] values);

        int index = FindSelectedIndex(current, values);
        int picked = EditorGUILayout.Popup(label, index, labels);
        ApplyPopupSelection(stringProp, current, values, picked);
    }

    static void DrawIdPopupAtRect(Rect rect, SerializedProperty stringProp, IReadOnlyList<string> registeredIds, GUIContent label)
    {
        if (stringProp == null)
            return;

        rect = EditorGUI.PrefixLabel(rect, label);

        string current = stringProp.stringValue ?? string.Empty;
        BuildPopupOptions(current, registeredIds, out string[] labels, out string[] values);

        int index = FindSelectedIndex(current, values);
        int picked = EditorGUI.Popup(rect, index, labels);
        ApplyPopupSelection(stringProp, current, values, picked);
    }

    static int FindSelectedIndex(string current, string[] values)
    {
        current = StringHelper.NormalizeResourceId(current);
        for (int i = 0; i < values.Length; i++)
        {
            if (string.Equals(values[i], current, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return 0;
    }

    static void ApplyPopupSelection(SerializedProperty stringProp, string current, string[] values, int picked)
    {
        if (picked < 0 || picked >= values.Length)
            return;

        string next = values[picked] ?? string.Empty;
        next = StringHelper.NormalizeResourceId(next);
        current = StringHelper.NormalizeResourceId(current);
        if (next != current)
            stringProp.stringValue = next;
    }

    static void BuildPopupOptions(string current, IReadOnlyList<string> registeredIds, out string[] labels, out string[] values)
    {
        var valueList = new List<string> { string.Empty };
        var labelList = new List<string> { "(无)" };

        for (int i = 0; i < registeredIds.Count; i++)
        {
            string id = registeredIds[i];
            if (string.IsNullOrEmpty(id))
                continue;
            valueList.Add(id);
            labelList.Add(id);
        }

        current = StringHelper.NormalizeResourceId(current);
        if (!string.IsNullOrEmpty(current) && !valueList.Contains(current))
        {
            valueList.Add(current);
            labelList.Add($"(未在 Manifest) {current}");
        }

        labels = labelList.ToArray();
        values = valueList.ToArray();
    }

    static List<string> CollectPrefabIds(string manifestFieldName, string scanFolderUnderAssets)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddManifestIds(set, manifestFieldName);
        AddPrefabIdsFromFolder(set, scanFolderUnderAssets);
        return SortIds(set);
    }

    static List<string> CollectWeaponConfigIds()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddManifestIds(set, nameof(GameResourceManifest.weaponConfigIds));
        AddConfigIdsFromFolder<WeaponConfig>(set, "Assets/Configs/Weapon");
        return SortIds(set);
    }

    static List<string> CollectDanmakuConfigIds()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddManifestIds(set, nameof(GameResourceManifest.danmakuConfigIds));
        AddConfigIdsFromFolder<DanmakuConfig>(set, "Assets/Configs/Danmaku");
        return SortIds(set);
    }

    static List<string> CollectEnemyConfigIds()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddManifestIds(set, nameof(GameResourceManifest.enemyConfigIds));
        AddConfigIdsFromFolder<EnemyConfig>(set, "Assets/Configs/Enemy");
        return SortIds(set);
    }

    static List<string> CollectDropItemConfigIds()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddManifestIds(set, nameof(GameResourceManifest.dropItemConfigIds));
        AddConfigIdsFromFolder<DropItemConfig>(set, "Assets/Configs/DropItem");
        return SortIds(set);
    }

    static List<string> CollectDanmakuEmitterConfigIds()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddManifestIds(set, nameof(GameResourceManifest.danmakuEmitterConfigIds));
        AddConfigIdsFromFolder<DanmakuEmitterConfig>(set, "Assets/Configs/DanmakuEmitter");
        return SortIds(set);
    }

    static void AddManifestIds(HashSet<string> set, string fieldName)
    {
        var manifest = LoadManifest();
        if (manifest == null)
            return;

        var so = new SerializedObject(manifest);
        var prop = so.FindProperty(fieldName);
        if (prop == null || !prop.isArray)
            return;

        for (int i = 0; i < prop.arraySize; i++)
        {
            string id = prop.GetArrayElementAtIndex(i).stringValue;
            if (!string.IsNullOrWhiteSpace(id))
                set.Add(StringHelper.NormalizeResourceId(id));
        }
    }

    static void AddPrefabIdsFromFolder(HashSet<string> set, string folderUnderAssets)
    {
        string folder = $"Assets/{folderUnderAssets.TrimStart('/')}";
        if (!Directory.Exists(folder))
            return;

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path))
                continue;
            string id = StringHelper.NormalizeResourceId(Path.GetFileNameWithoutExtension(path));
            if (!string.IsNullOrEmpty(id))
                set.Add(id);
        }
    }

    static void AddConfigIdsFromFolder<T>(HashSet<string> set, string folderUnderAssets) where T : GameConfig
    {
        string folder = folderUnderAssets;
        if (!folder.StartsWith("Assets/", StringComparison.Ordinal))
            folder = "Assets/" + folderUnderAssets.TrimStart('/');

        if (!Directory.Exists(folder))
            return;

        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var cfg = AssetDatabase.LoadAssetAtPath<T>(path);
            if (cfg == null)
                continue;
            string id = cfg.ConfigId;
            if (!string.IsNullOrEmpty(id))
                set.Add(id);
        }
    }

    static List<string> SortIds(HashSet<string> set)
    {
        var list = new List<string>(set);
        list.Sort(StringComparer.OrdinalIgnoreCase);
        return list;
    }

    static GameResourceManifest LoadManifest()
    {
        return AssetDatabase.LoadAssetAtPath<GameResourceManifest>(ManifestAssetPath);
    }
}
#endif
