#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DanmakuEmitterConfigIdAttribute))]
public sealed class DanmakuEmitterConfigIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        ResourceIdEditorPicker.DrawDanmakuEmitterConfigIdAtRect(position, property, label);
        EditorGUI.EndProperty();
    }
}
#endif
