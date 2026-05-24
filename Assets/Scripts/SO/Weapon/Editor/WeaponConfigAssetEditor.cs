#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponConfig))]
[CanEditMultipleObjects]
public class WeaponConfigAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "danmakuEmitterConfigIds",
            "description",
            "secondaryEmitters",
            "weaponPrefabId");

        var weaponPrefabId = serializedObject.FindProperty("weaponPrefabId");
        EditorGUILayout.PropertyField(weaponPrefabId);
        ResourceIdEditorPicker.DrawPoolPrefabIdField(weaponPrefabId, E_PoolCategory.Weapon);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
