#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PoolPrefabIdAttribute))]
public sealed class PoolPrefabIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        E_PoolCategory category = ResolveCategory(property);
        ResourceIdEditorPicker.DrawPoolPrefabIdAtRect(position, property, label, category);

        EditorGUI.EndProperty();
    }

    E_PoolCategory ResolveCategory(SerializedProperty property)
    {
        if (fieldInfo != null &&
            fieldInfo.GetCustomAttributes(typeof(PoolPrefabIdAttribute), false) is PoolPrefabIdAttribute[] attrs &&
            attrs.Length > 0 &&
            attrs[0].HasExplicitCategory)
            return attrs[0].Category;

        return ResourceIdEditorPicker.ResolvePoolCategoryFromProperty(property);
    }
}
#endif
