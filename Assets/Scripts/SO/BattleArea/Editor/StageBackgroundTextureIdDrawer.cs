#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(StageBackgroundTextureIdAttribute))]
public sealed class StageBackgroundTextureIdDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
        EditorGUIUtility.singleLineHeight;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        ResourceIdEditorPicker.DrawStageBackgroundTextureIdAtRect(position, property, label);
        EditorGUI.EndProperty();
    }
}
#endif
