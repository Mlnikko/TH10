#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DropItemConfigIdAttribute))]
public sealed class DropItemConfigIdDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property != null && property.isArray)
            return EditorGUIUtility.singleLineHeight;

        return EditorGUIUtility.singleLineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property != null && property.isArray)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
        else
        {
            ResourceIdEditorPicker.DrawDropItemConfigIdAtRect(position, property, label);
        }

        EditorGUI.EndProperty();
    }
}
#endif
