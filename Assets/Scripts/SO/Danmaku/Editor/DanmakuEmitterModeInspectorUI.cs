#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 按 <see cref="EmitMode"/> 过滤 Line/Arc/Wave/Grain 模式参数在 Inspector 中的显示。
/// </summary>
public static class DanmakuEmitterModeInspectorUI
{
    public const string ViewerEmitModeField = "emitterType";
    public const string ConfigEmitModeField = nameof(DanmakuEmitterConfig.emitMode);

    static readonly string[] ViewerDisplayPropertyNames =
    {
        "displaySprite",
        "displaySelfSpinDegreesPerSecond",
        "displayScaleMin",
        "displayScaleMax",
        "displayScaleCyclesPerSecond",
    };

    const string ViewerEmitterCampField = "emitterCamp";

    public static EmitMode ReadEmitMode(SerializedObject serializedObject, string emitModePropertyName)
    {
        if (serializedObject == null)
            return EmitMode.None;

        var modeProp = serializedObject.FindProperty(emitModePropertyName);
        if (modeProp == null || modeProp.propertyType != SerializedPropertyType.Enum)
            return EmitMode.None;

        return (EmitMode)modeProp.enumValueIndex;
    }

    public static bool IsModeConfigProperty(string propertyName) =>
        propertyName == nameof(DanmakuEmitterConfig.lineModeConfig)
        || propertyName == nameof(DanmakuEmitterConfig.arcModeConfig)
        || propertyName == nameof(DanmakuEmitterConfig.waveModeConfig)
        || propertyName == nameof(DanmakuEmitterConfig.grainModeConfig);

    public static string GetModeConfigPropertyName(EmitMode mode) =>
        mode switch
        {
            EmitMode.Line => nameof(DanmakuEmitterConfig.lineModeConfig),
            EmitMode.Arc => nameof(DanmakuEmitterConfig.arcModeConfig),
            EmitMode.Wave => nameof(DanmakuEmitterConfig.waveModeConfig),
            EmitMode.Grain => nameof(DanmakuEmitterConfig.grainModeConfig),
            _ => null,
        };

    public static bool ShouldShowModeProperty(EmitMode mode, string propertyName) =>
        propertyName switch
        {
            nameof(DanmakuEmitterConfig.lineModeConfig) => mode == EmitMode.Line,
            nameof(DanmakuEmitterConfig.arcModeConfig) => mode == EmitMode.Arc,
            nameof(DanmakuEmitterConfig.waveModeConfig) => mode == EmitMode.Wave,
            nameof(DanmakuEmitterConfig.grainModeConfig) => mode == EmitMode.Grain,
            _ => true,
        };

    public static bool ShouldSkipProperty(
        SerializedObject serializedObject,
        string propertyName,
        string emitModePropertyName)
    {
        if (!IsModeConfigProperty(propertyName))
            return false;

        var mode = ReadEmitMode(serializedObject, emitModePropertyName);
        return !ShouldShowModeProperty(mode, propertyName);
    }

    public static void DrawEmitModeHint(EmitMode mode)
    {
        if (mode != EmitMode.None)
            return;

        EditorGUILayout.HelpBox(
            "发射模式为 None 时不会发射弹幕；选择 Line / Arc / Wave / Grain 后显示对应参数。",
            MessageType.Info);
    }

    /// <summary>
    /// 发射模式枚举 + 当前模式参数（与选项紧挨，不分散到 Inspector 其它位置）。
    /// </summary>
    public static bool DrawEmitModeSection(
        SerializedObject serializedObject,
        string emitModePropertyName,
        bool drawSectionHeader = true)
    {
        if (serializedObject == null)
            return false;

        if (drawSectionHeader)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("发射模式", EditorStyles.boldLabel);
        }

        EditorGUI.indentLevel++;

        var emitModeProp = serializedObject.FindProperty(emitModePropertyName);
        EditorGUI.BeginChangeCheck();
        if (emitModeProp != null)
            EditorGUILayout.PropertyField(emitModeProp, new GUIContent("发射模式"));

        bool changed = EditorGUI.EndChangeCheck();

        var mode = ReadEmitMode(serializedObject, emitModePropertyName);
        DrawEmitModeHint(mode);
        DrawActiveModeConfigProperty(serializedObject, mode);

        EditorGUI.indentLevel--;
        return changed;
    }

    static void DrawActiveModeConfigProperty(SerializedObject serializedObject, EmitMode mode)
    {
        string modeConfigName = GetModeConfigPropertyName(mode);
        if (string.IsNullOrEmpty(modeConfigName))
            return;

        var modeConfigProp = serializedObject.FindProperty(modeConfigName);
        if (modeConfigProp == null)
            return;

        EditorGUILayout.PropertyField(modeConfigProp, true);
    }

    static void DrawNamedProperties(SerializedObject serializedObject, IReadOnlyList<string> propertyNames)
    {
        if (serializedObject == null || propertyNames == null)
            return;

        for (int i = 0; i < propertyNames.Count; i++)
        {
            var prop = serializedObject.FindProperty(propertyNames[i]);
            if (prop != null)
                EditorGUILayout.PropertyField(prop, true);
        }
    }

    static HashSet<string> BuildViewerExcludeSet(IReadOnlyCollection<string> additionalExcludes)
    {
        var set = new HashSet<string>
        {
            "m_Script",
            "emitterConfig",
            ViewerEmitModeField,
            nameof(DanmakuEmitterConfig.lineModeConfig),
            nameof(DanmakuEmitterConfig.arcModeConfig),
            nameof(DanmakuEmitterConfig.waveModeConfig),
            nameof(DanmakuEmitterConfig.grainModeConfig),
            ViewerEmitterCampField,
        };

        for (int i = 0; i < ViewerDisplayPropertyNames.Length; i++)
            set.Add(ViewerDisplayPropertyNames[i]);

        if (additionalExcludes == null)
            return set;

        foreach (string name in additionalExcludes)
        {
            if (!string.IsNullOrEmpty(name))
                set.Add(name);
        }

        return set;
    }

    /// <summary>
    /// Viewer：阵营 → 发射器显示（含 Sprite/自转/缩放 + 发射模式与参数）→ 其余字段。
    /// </summary>
    public static bool DrawViewerInspector(
        SerializedObject serializedObject,
        IReadOnlyCollection<string> additionalExcludes = null)
    {
        if (serializedObject == null)
            return false;

        bool changed = false;

        var campProp = serializedObject.FindProperty(ViewerEmitterCampField);
        if (campProp != null)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(campProp, true);
            changed |= EditorGUI.EndChangeCheck();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("发射器显示", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUI.BeginChangeCheck();
        DrawNamedProperties(serializedObject, ViewerDisplayPropertyNames);
        changed |= DrawEmitModeSection(serializedObject, ViewerEmitModeField, drawSectionHeader: false);
        EditorGUI.indentLevel--;
        changed |= EditorGUI.EndChangeCheck();

        var exclude = BuildViewerExcludeSet(additionalExcludes);
        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (exclude.Contains(prop.name))
                continue;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prop, true);
            changed |= EditorGUI.EndChangeCheck();
        }

        return changed;
    }

    /// <summary>
    /// 绘制除排除项外的可见字段，并按当前发射模式隐藏无关 *ModeConfig（用于 Config SO 等顺序固定的 Inspector）。
    /// </summary>
    public static void DrawFilteredProperties(
        SerializedObject serializedObject,
        IReadOnlyCollection<string> excludePropertyNames,
        string emitModePropertyName)
    {
        if (serializedObject == null)
            return;

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script")
                continue;

            if (excludePropertyNames != null && excludePropertyNames.Contains(prop.name))
                continue;

            if (prop.name == emitModePropertyName)
            {
                EditorGUILayout.PropertyField(prop);
                var mode = ReadEmitMode(serializedObject, emitModePropertyName);
                DrawEmitModeHint(mode);
                DrawActiveModeConfigProperty(serializedObject, mode);
                continue;
            }

            if (ShouldSkipProperty(serializedObject, prop.name, emitModePropertyName))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }
    }
}
#endif
