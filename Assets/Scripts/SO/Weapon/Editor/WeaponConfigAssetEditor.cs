#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponConfig))]
[CanEditMultipleObjects]
public class WeaponConfigAssetEditor : Editor
{
    const string PowerPrimarySlowField = "powerPrimarySlowLayouts";
    const string PowerSecondaryField = "powerSecondaryLayouts";
    const string PrimaryEmittersField = "primaryEmitters";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "武器池化预制体共用 WeaponPrefabArchetypes.Layout；发射器挂点由 WeaponRuntimeLayoutView 按本配置动态创建。",
            MessageType.Info);

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;
        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (prop.name == "m_Script")
            {
                EditorGUILayout.PropertyField(prop);
                continue;
            }

            if (prop.name == "weaponPrefabId")
            {
                ResourceIdEditorPicker.DrawPoolPrefabIdField(prop, E_PoolCategory.Weapon);
                continue;
            }

            if (prop.name == PrimaryEmittersField)
            {
                var powerSlow = serializedObject.FindProperty(PowerPrimarySlowField);
                DrawPrimaryEmitters(prop, powerSlow);
                continue;
            }

            if (prop.name == PowerPrimarySlowField)
            {
                ResourceIdEditorPicker.DrawWeaponPowerPrimarySlowLayouts(prop);
                continue;
            }

            if (prop.name == PowerSecondaryField)
            {
                ResourceIdEditorPicker.DrawWeaponPowerSecondaryLayouts(prop);
                continue;
            }

            if (prop.name == "secondaryEmitters"
                || prop.name == "danmakuEmitterConfigIds"
                || prop.name == "description")
                continue;

            if (prop.propertyPath.StartsWith(PowerPrimarySlowField + ".", System.StringComparison.Ordinal)
                || prop.propertyPath.StartsWith(PowerSecondaryField + ".", System.StringComparison.Ordinal)
                || prop.propertyPath.StartsWith(PrimaryEmittersField + ".", System.StringComparison.Ordinal))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    internal static void DrawPrimaryEmitters(SerializedProperty groupProp, SerializedProperty powerPrimarySlowProp)
    {
        if (groupProp == null)
            return;

        EditorGUILayout.LabelField("主发射器", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var normal = groupProp.FindPropertyRelative(nameof(WeaponPrimaryEmitterGroup.normal));
        var slowId = groupProp.FindPropertyRelative(nameof(WeaponPrimaryEmitterGroup.slowModeDanmakuEmitterConfigId));

        if (normal != null)
            ResourceIdEditorPicker.DrawWeaponEmitterSlot(normal, ResourceIdEditorPicker.GetDanmakuEmitterConfigIds(), "通常模式");

        bool hasPowerSlow = powerPrimarySlowProp != null && powerPrimarySlowProp.arraySize > 0;
        if (!hasPowerSlow && slowId != null)
        {
            EditorGUILayout.HelpBox(
                "已配置「低速主炮（按 Power）」时，将忽略下方的单一 slowMode 发射器 Id。",
                MessageType.None);
            ResourceIdEditorPicker.DrawDanmakuEmitterConfigIdField(slowId);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);
    }
}
#endif
