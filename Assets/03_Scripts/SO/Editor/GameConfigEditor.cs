using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(GameConfig), true)]
public class GameConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 先绘制默认 Inspector（对多选也安全）
        base.OnInspectorGUI();

        GUILayout.Space(10);

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            EditorGUILayout.HelpBox("Addressables 未初始化！", MessageType.Error);
            if (GUILayout.Button("Initialize Addressables"))
            {
                AddressableAssetSettings.Create(
                    AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                    AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                    false,
                    true
                );
            }
            return;
        }

        // 多选处理
        if (serializedObject.isEditingMultipleObjects)
        {
            EditorGUILayout.HelpBox($"选中了 {targets.Length} 个 GameConfig 资源", MessageType.Info);

            if (GUILayout.Button("为所有设置 Addressable Key"))
            {
                bool hasError = false;
                foreach (Object obj in targets)
                {
                    if (!(obj is GameConfig config)) continue;

                    string assetPath = AssetDatabase.GetAssetPath(config);
                    if (string.IsNullOrEmpty(assetPath))
                    {
                        Debug.LogError($"无效资源路径: {obj.name}", obj);
                        hasError = true;
                        continue;
                    }

                    string id = !string.IsNullOrWhiteSpace(config.ConfigId)
                        ? config.ConfigId
                        : Path.GetFileNameWithoutExtension(assetPath);

                    string key = config.AddressableKeyPrefix + id;
                    string guid = AssetDatabase.AssetPathToGUID(assetPath);

                    var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
                    entry.address = key;
                }

                if (!hasError)
                {
                    EditorUtility.SetDirty(settings);
                    Logger.Info($"已为 {targets.Length} 个配置设置 Addressable Key", LogTag.Config);
                }
            }
        }
        else
        {
            // 单选逻辑（原逻辑）
            GameConfig config = (GameConfig)target;
            string assetPath = AssetDatabase.GetAssetPath(config);

            if (string.IsNullOrEmpty(assetPath))
            {
                EditorGUILayout.HelpBox("资源路径无效。", MessageType.Warning);
                return;
            }

            string id = !string.IsNullOrWhiteSpace(config.ConfigId)
                ? config.ConfigId
                : Path.GetFileNameWithoutExtension(assetPath);

            string key = config.AddressableKeyPrefix + id;
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.FindAssetEntry(guid);
            bool isCorrect = entry != null && entry.address == key;

            if (isCorrect)
            {
                EditorGUILayout.HelpBox($"Key: {key}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"将设置 Key 为:\n{key}", MessageType.None);
                if (GUILayout.Button("Set Addressable Key"))
                {
                    var defaultGroup = settings.DefaultGroup;
                    if (defaultGroup == null)
                    {
                        Debug.LogError("默认分组不存在！", settings);
                        return;
                    }

                    var newEntry = settings.CreateOrMoveEntry(guid, defaultGroup);
                    newEntry.address = key;

                    EditorUtility.SetDirty(settings);
                    Logger.Info($"Set Addressable Key: {key}", LogTag.Config);
                }
            }
        }
    }
}