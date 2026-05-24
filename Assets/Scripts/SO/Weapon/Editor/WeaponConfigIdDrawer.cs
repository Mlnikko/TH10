#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(WeaponConfigIdAttribute))]
public sealed class WeaponConfigIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        ResourceIdEditorPicker.DrawWeaponConfigIdAtRect(position, property, label);
        EditorGUI.EndProperty();
    }
}
#endif
