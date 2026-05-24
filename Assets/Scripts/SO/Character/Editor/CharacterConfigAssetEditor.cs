#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterConfig))]
[CanEditMultipleObjects]
public class CharacterConfigAssetEditor : Editor
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

            if (prop.name == nameof(CharacterConfig.weaponConfigIds))
            {
                ResourceIdEditorPicker.DrawWeaponConfigIdArray(prop);
                continue;
            }

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
