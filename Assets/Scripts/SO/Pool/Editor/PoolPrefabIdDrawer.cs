#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PoolPrefabIdAttribute))]
public sealed class PoolPrefabIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        E_PoolCategory category = ResourceIdEditorPicker.ResolvePoolCategoryFromProperty(property);
        ResourceIdEditorPicker.DrawPoolPrefabIdAtRect(position, property, label, category);

        EditorGUI.EndProperty();
    }
}
#endif
