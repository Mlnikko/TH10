#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>从 Boss 关联的敌人预制体 Animator Controller 列出状态名（非 AnimationClip）。</summary>
static class BossAnimatorStatePicker
{
    const string EnemyConfigFolder = "Assets/Configs/Enemy";
    const string EnemyPrefabFolder = "Assets/Prefabs/Enemy";

    public static void DrawAnimatorStateSection(
        SerializedObject encounterSo,
        SerializedProperty enemyConfigIdProp,
        string sectionTitle,
        (SerializedProperty prop, string label)[] fields)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(sectionTitle, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "填写 Animator Controller 中的状态名（运行时 MidBossUpdater 调用 Animator.Play）。"
            + " 不是 AnimationClip 资源。",
            MessageType.None);

        string enemyConfigId = enemyConfigIdProp?.stringValue;
        var stateNames = CollectAnimatorStateNames(enemyConfigId);

        encounterSo.Update();
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (field.prop == null)
                continue;
            DrawAnimatorStatePopup(field.prop, stateNames, field.label);
        }

        encounterSo.ApplyModifiedProperties();
    }

    static void DrawAnimatorStatePopup(
        SerializedProperty stringProp,
        IReadOnlyList<string> stateNames,
        string label)
    {
        if (stringProp == null || stringProp.propertyType != SerializedPropertyType.String)
            return;

        string current = stringProp.stringValue ?? string.Empty;

        if (stateNames == null || stateNames.Count == 0)
        {
            stringProp.stringValue = EditorGUILayout.TextField(
                new GUIContent(label, "Animator 状态名；请先配置敌人 Config Id 以启用下拉"),
                current);
            return;
        }

        var options = new List<string> { "（不切换）" };
        options.AddRange(stateNames);
        int selected = 0;
        for (int i = 0; i < stateNames.Count; i++)
        {
            if (stateNames[i] == current)
            {
                selected = i + 1;
                break;
            }
        }

        if (!string.IsNullOrEmpty(current) && selected == 0)
        {
            options.Add($"（当前）{current}");
            selected = options.Count - 1;
        }

        EditorGUI.BeginChangeCheck();
        int next = EditorGUILayout.Popup(new GUIContent(label, "Animator Controller 状态名"), selected, options.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            if (next <= 0)
                stringProp.stringValue = string.Empty;
            else if (next <= stateNames.Count)
                stringProp.stringValue = stateNames[next - 1];
        }
    }

    public static List<string> CollectAnimatorStateNames(string enemyConfigId)
    {
        var names = new List<string>();
        if (string.IsNullOrWhiteSpace(enemyConfigId))
            return names;

        var enemyCfg = LoadEnemyConfig(enemyConfigId);
        if (enemyCfg == null || string.IsNullOrWhiteSpace(enemyCfg.enemyPrefabId))
            return names;

        var prefab = LoadEnemyPrefab(enemyCfg.enemyPrefabId);
        if (prefab == null)
            return names;

        var animator = prefab.GetComponent<Animator>();
        if (animator == null)
            return names;

        var controller = ResolveAnimatorController(animator.runtimeAnimatorController);
        if (controller == null)
            return names;

        var set = new HashSet<string>();
        for (int i = 0; i < controller.layers.Length; i++)
            CollectFromStateMachine(controller.layers[i].stateMachine, set);

        names.AddRange(set);
        names.Sort();
        return names;
    }

    static void CollectFromStateMachine(AnimatorStateMachine stateMachine, HashSet<string> names)
    {
        if (stateMachine == null)
            return;

        foreach (var child in stateMachine.states)
        {
            if (!string.IsNullOrEmpty(child.state.name))
                names.Add(child.state.name);
        }

        foreach (var child in stateMachine.stateMachines)
            CollectFromStateMachine(child.stateMachine, names);
    }

    static AnimatorController ResolveAnimatorController(RuntimeAnimatorController runtime)
    {
        if (runtime == null)
            return null;

        if (runtime is AnimatorController ctrl)
            return ctrl;

        if (runtime is AnimatorOverrideController overrideCtrl)
            return overrideCtrl.runtimeAnimatorController as AnimatorController;

        return null;
    }

    static EnemyConfig LoadEnemyConfig(string configId)
    {
        string id = configId.ToLowerInvariantTrimmed();
        string[] guids = AssetDatabase.FindAssets($"t:{nameof(EnemyConfig)}", new[] { EnemyConfigFolder });
        for (int i = 0; i < guids.Length; i++)
        {
            var path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var cfg = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            if (cfg != null && cfg.ConfigId == id)
                return cfg;
        }

        return null;
    }

    static GameObject LoadEnemyPrefab(string prefabId)
    {
        string id = prefabId.ToLowerInvariantTrimmed();
        string path = $"{EnemyPrefabFolder}/{id}.prefab";
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }
}
#endif
