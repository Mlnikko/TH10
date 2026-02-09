using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

[CustomEditor(typeof(GameResourceManifest))]
public class GameResourceManifestEditor : Editor
{
    static readonly Dictionary<string, (string folder, System.Type assetType)> s_FieldRules = new()
    {
        // Configs
        { nameof(GameResourceManifest.characterConfigIds),       ("/Configs/Character/", typeof(ScriptableObject)) },
        { nameof(GameResourceManifest.weaponConfigIds),          ("/Configs/Weapon/", typeof(ScriptableObject)) },
        { nameof(GameResourceManifest.danmakuConfigIds),         ("/Configs/Danmaku/", typeof(ScriptableObject)) },
        { nameof(GameResourceManifest.danmakuEmitterConfigIds),  ("/Configs/DanmakuEmitter/", typeof(ScriptableObject)) },
        { nameof(GameResourceManifest.enemyConfigIds),           ("/Configs/Enemy/", typeof(ScriptableObject)) },

        // Prefabs
        { nameof(GameResourceManifest.characterPrefabIds),       ("/Prefabs/Character/", typeof(GameObject)) },
        { nameof(GameResourceManifest.enemyPrefabIds),           ("/Prefabs/Enemy/", typeof(GameObject)) },
        { nameof(GameResourceManifest.danmakuPrefabIds),         ("/Prefabs/Danmaku/", typeof(GameObject)) },
        { nameof(GameResourceManifest.danmakuEmitterPrefabIds),  ("/Prefabs/DanmakuEmitter/", typeof(GameObject)) },
        { nameof(GameResourceManifest.effectPrefabIds),          ("/Prefabs/Effect/", typeof(GameObject)) },

        // Atlases
        { nameof(GameResourceManifest.atlases),                  ("/Art/Atlas/", typeof(SpriteAtlas)) },

        // Textures
        { nameof(GameResourceManifest.characterImages),          ("/Art/Texture/Character/", typeof(Texture2D)) },
    };


    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("自动识别填充资源ID"))
            FillResources();
    }

    void FillResources()
    {
        var manifest = (GameResourceManifest)target;
        Undo.RecordObject(manifest, "Auto-fill Resource Manifest");

        foreach (var kvp in s_FieldRules)
        {
            string fieldName = kvp.Key;
            string folderPath = $"Assets{kvp.Value.folder}";
            System.Type assetType = kvp.Value.assetType;

            // 获取字段的 SerializedProperty（用于修改数组）
            SerializedProperty arrayProp = serializedObject.FindProperty(fieldName);
            if (arrayProp == null)
            {
                Debug.LogWarning($"Field not found: {fieldName}");
                continue;
            }

            // 清空数组
            arrayProp.ClearArray();

            if (!Directory.Exists(folderPath))
            {
                Logger.Warn($"Folder not found, skipping: {folderPath}", LogTag.File, manifest);
                continue;
            }

            // 批量查找资源
            string[] guids = AssetDatabase.FindAssets($"t:{assetType.Name}", new[] { folderPath });
            var ids = new List<string>();

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                string fileName = Path.GetFileNameWithoutExtension(assetPath);
                string id = fileName.ToLowerInvariant();
                if (!ids.Contains(id))
                    ids.Add(id);
            }

            // 填入 SerializedProperty
            arrayProp.arraySize = ids.Count;
            for (int i = 0; i < ids.Count; i++)
            {
                arrayProp.GetArrayElementAtIndex(i).stringValue = ids[i];
            }
        }

        serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        Logger.Debug("资源清单自动填充完成！（基于扁平化字段规则）", LogTag.Resource, manifest);
    }
}