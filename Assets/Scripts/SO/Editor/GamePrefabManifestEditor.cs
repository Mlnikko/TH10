using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GamePrefabManifest))]
public class GamePrefabManifestEditor : Editor
{
    const string PREFAB_SEARCH_PATH = "Assets/Prefabs";

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(10);
        if (GUILayout.Button("自动识别填充预制体ID"))
        {
            AutoFillPrefabIds();
        }
    }

    void AutoFillPrefabIds()
    {
        var manifest = (GamePrefabManifest)target;

        // 按文件夹分类收集 Prefab 路径
        var danmakuPrefabIds = new List<string>();
        var characterPrefabIds = new List<string>();

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PREFAB_SEARCH_PATH });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
            string lowerPath = path.ToLower();

            if (lowerPath.Contains("/danmakus/"))
                danmakuPrefabIds.Add(fileNameWithoutExt);
            else if (lowerPath.Contains("/characters/"))
                characterPrefabIds.Add(fileNameWithoutExt);

        }

        // 排序（可选）
        danmakuPrefabIds.Distinct().OrderBy(x => x).ToList();
        characterPrefabIds.Distinct().OrderBy(x => x).ToList();

        // 应用到 manifest
        Undo.RecordObject(manifest, "Auto Fill Prefab Paths");
        manifest.danmakuPrefabIds = danmakuPrefabIds.ToArray();
        manifest.characterPrefabIds = characterPrefabIds.ToArray();

        EditorUtility.SetDirty(manifest);
        AssetDatabase.SaveAssets();

        Logger.Info($"Auto-filled prefabs:\n" +
                  $"Danmakus: {danmakuPrefabIds.Count}"+
                  $"Characters: {characterPrefabIds.Count}"
                  );
    }
}