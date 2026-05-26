#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DropItemConfig))]
[CanEditMultipleObjects]
public class DropItemConfigAssetEditor : Editor
{
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

            if (prop.name == nameof(DropItemConfig.pickupPrefabId))
            {
                ResourceIdEditorPicker.DrawDropItemPrefabIdField(prop);
                continue;
            }

            if (prop.name == nameof(DropItemConfig.motionMode))
            {
                EditorGUILayout.PropertyField(prop);
                var mode = (E_DropMotionMode)prop.enumValueIndex;
                if (mode == E_DropMotionMode.VerticalToss)
                {
                    DrawField(nameof(DropItemConfig.initialUpSpeed));
                    DrawField(nameof(DropItemConfig.fallGravity));
                    DrawField(nameof(DropItemConfig.maxFallSpeed));
                    DrawField(nameof(DropItemConfig.riseSpinDegreesPerSecond));
                }
                else if (mode == E_DropMotionMode.DirectionalBurstThenFall)
                {
                    DrawField(nameof(DropItemConfig.burstInitialSpeed));
                    DrawField(nameof(DropItemConfig.burstDirection));
                    DrawField(nameof(DropItemConfig.burstDeceleration));
                    DrawField(nameof(DropItemConfig.fallSpeedAfterBurst));
                }

                continue;
            }

            if (IsBakedOrHiddenMotionField(prop.name))
                continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    void DrawField(string fieldName)
    {
        var p = serializedObject.FindProperty(fieldName);
        if (p != null)
            EditorGUILayout.PropertyField(p);
    }

    static bool IsBakedOrHiddenMotionField(string name) => name switch
    {
        nameof(DropItemConfig.initialUpPerFrame) => true,
        nameof(DropItemConfig.gravityPerFrame) => true,
        nameof(DropItemConfig.maxFallPerFrame) => true,
        nameof(DropItemConfig.spinRadPerFrame) => true,
        nameof(DropItemConfig.burstInitialPerFrame) => true,
        nameof(DropItemConfig.burstDecelPerFrame) => true,
        nameof(DropItemConfig.burstDirX) => true,
        nameof(DropItemConfig.burstDirY) => true,
        nameof(DropItemConfig.fallVyAfterBurstPerFrame) => true,
        nameof(DropItemConfig.initialUpSpeed) => true,
        nameof(DropItemConfig.fallGravity) => true,
        nameof(DropItemConfig.maxFallSpeed) => true,
        nameof(DropItemConfig.riseSpinDegreesPerSecond) => true,
        nameof(DropItemConfig.burstInitialSpeed) => true,
        nameof(DropItemConfig.burstDirection) => true,
        nameof(DropItemConfig.burstDeceleration) => true,
        nameof(DropItemConfig.fallSpeedAfterBurst) => true,
        _ => false,
    };
}
#endif
