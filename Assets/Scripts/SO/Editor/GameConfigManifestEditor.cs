using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

[CustomEditor(typeof(GameConfigManifest))]
public class GameConfigManifestEditor : Editor
{
    const string SEARCH_PATH = "Assets/Configs";

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GUILayout.Space(10);
        if (GUILayout.Button("自动识别填充配置ID"))
        {
            AutoFillConfigIds();
        }
    }

    void AutoFillConfigIds()
    {
        var checklist = (GameConfigManifest)target;

        // 查找所有 GameConfig 子类的 ScriptableObject
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(GameConfig)}", new[] { SEARCH_PATH });

        var danmakuIds = new List<string>();
        var emitterIds = new List<string>();
        var characterIds = new List<string>();
        var weaponIds = new List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameConfig>(path);

            if (asset == null || asset == checklist) continue; // 跳过自己

            string id = asset.ConfigId;

            switch (asset)
            {
                case DanmakuConfig _: danmakuIds.Add(id); break;
                case DanmakuEmitterConfig _: emitterIds.Add(id); break;
                case CharacterConfig _: characterIds.Add(id); break;
                case WeaponConfig _: weaponIds.Add(id); break;
                    // 可扩展其他类型
            }
        }

        // 去重 + 排序（可选）
        danmakuIds = danmakuIds.Distinct().OrderBy(x => x).ToList();
        emitterIds = emitterIds.Distinct().OrderBy(x => x).ToList();
        characterIds = characterIds.Distinct().OrderBy(x => x).ToList();
        weaponIds = weaponIds.Distinct().OrderBy(x => x).ToList();

        // 应用到 checklist
        Undo.RecordObject(checklist, "Auto Fill Config IDs");
        checklist.danmakuConfigIds = danmakuIds.ToArray();
        checklist.emitterConfigIds = emitterIds.ToArray();
        checklist.characterConfigIds = characterIds.ToArray();
        checklist.weaponConfigIds = weaponIds.ToArray();

        EditorUtility.SetDirty(checklist);
        AssetDatabase.SaveAssets();

        Logger.Info($"Auto-filled configs:\n" +
                  $"Danmaku: {danmakuIds.Count}, " +
                  $"Emitter: {emitterIds.Count}, " +
                  $"Character: {characterIds.Count}, " +
                  $"Weapon: {weaponIds.Count}", LogTag.Config);
    }
}