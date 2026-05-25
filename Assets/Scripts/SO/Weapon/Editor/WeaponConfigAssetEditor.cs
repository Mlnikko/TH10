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

            if (prop.name == nameof(WeaponConfig.slowModeLayout))
            {
                DrawSlowModeLayout(prop);
                continue;
            }

            if (prop.name == "secondaryEmitters"
                || prop.name == "danmakuEmitterConfigIds"
                || prop.name == "description")
                continue;

            if (prop.propertyPath.StartsWith(PowerPrimarySlowField + ".", System.StringComparison.Ordinal)
                || prop.propertyPath.StartsWith(PowerSecondaryField + ".", System.StringComparison.Ordinal)
                || prop.propertyPath.StartsWith(PrimaryEmittersField + ".", System.StringComparison.Ordinal)
                || prop.propertyPath.StartsWith(nameof(WeaponConfig.slowModeLayout) + ".", System.StringComparison.Ordinal))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    internal static void DrawSlowModeLayout(SerializedProperty layoutProp)
    {
        if (layoutProp == null)
            return;

        EditorGUILayout.LabelField("低速发射器布局", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        var primaryMode = layoutProp.FindPropertyRelative(nameof(WeaponSlowModeLayoutConfig.primarySlowPositionMode));
        var secondaryMode = layoutProp.FindPropertyRelative(nameof(WeaponSlowModeLayoutConfig.secondarySlowPositionMode));

        EditorGUILayout.PropertyField(primaryMode, new GUIContent("主炮低速模式"));
        EditorGUILayout.PropertyField(secondaryMode, new GUIContent("副炮低速模式"));

        var secondaryModeValue = (E_WeaponSlowSlotPositionMode)secondaryMode.enumValueIndex;
        if (secondaryModeValue == E_WeaponSlowSlotPositionMode.ConvergeToPlayer)
        {
            EditorGUILayout.PropertyField(
                layoutProp.FindPropertyRelative(nameof(WeaponSlowModeLayoutConfig.secondarySlotConverge)));
            EditorGUILayout.PropertyField(
                layoutProp.FindPropertyRelative(nameof(WeaponSlowModeLayoutConfig.secondarySlotConvergeSpeed)));
        }
        else if (secondaryModeValue == E_WeaponSlowSlotPositionMode.TrailFollowWhileFast)
        {
            EditorGUILayout.HelpBox(
                "通常模式：副炮偏移随移动方向展开；进入低速后冻结当前相对偏移，仅随玩家平移。",
                MessageType.None);
            EditorGUILayout.PropertyField(
                layoutProp.FindPropertyRelative(nameof(WeaponSlowModeLayoutConfig.secondaryTrailSpreadPerSpeed)));
            EditorGUILayout.PropertyField(
                layoutProp.FindPropertyRelative(nameof(WeaponSlowModeLayoutConfig.secondaryTrailMaxOffset)));
            EditorGUILayout.PropertyField(
                layoutProp.FindPropertyRelative(nameof(WeaponSlowModeLayoutConfig.secondaryTrailCatchUpSpeed)));
        }
        else if (secondaryModeValue == E_WeaponSlowSlotPositionMode.WorldAnchorWhileSlow)
        {
            EditorGUILayout.HelpBox(
                "进入低速时副炮留在当前世界坐标；退出低速后回到配置槽位。",
                MessageType.None);
        }

        if (secondaryModeValue != E_WeaponSlowSlotPositionMode.ConvergeToPlayer)
        {
            EditorGUILayout.PropertyField(
                layoutProp.FindPropertyRelative(nameof(WeaponSlowModeLayoutConfig.secondaryReturnToSlotSpeed)),
                new GUIContent("退出低速回位速度"));
        }

        EditorGUILayout.PropertyField(
            layoutProp.FindPropertyRelative(nameof(WeaponSlowModeLayoutConfig.primarySlotConverge)),
            new GUIContent("主炮收束比例（Converge 模式）"));

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(4);
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
