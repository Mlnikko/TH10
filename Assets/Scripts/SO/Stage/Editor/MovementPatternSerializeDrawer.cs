#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(MovementPatternSerializeAttribute))]
public sealed class MovementPatternSerializeDrawer : PropertyDrawer
{
    static readonly (string menuPath, Type type)[] ConcreteTypes =
    {
        ("无", null),
        ("静态", typeof(StaticMovementData)),
        ("直线匀速", typeof(LinearMovementData)),
        ("瞄准玩家直线", typeof(AimedLinearMovementData)),
        ("直线 + 正弦摆动", typeof(SineMovementData)),
        ("圆周运动", typeof(CircularMovementData)),
        ("三次贝塞尔", typeof(BezierCubicMovementData)),
        ("折线路径", typeof(WaypointPathMovementData)),
    };

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.ManagedReference)
            return EditorGUI.GetPropertyHeight(property, label, true);

        float h = EditorGUIUtility.singleLineHeight;
        if (property.isExpanded)
        {
            h += EditorGUIUtility.standardVerticalSpacing;
            if (property.managedReferenceValue == null)
            {
                float innerW = Mathf.Max(120f, EditorGUIUtility.currentViewWidth * 0.55f);
                h += EditorStyles.helpBox.CalcHeight(new GUIContent("点击「类型…」选择运动模式。"), innerW);
            }
            else
            {
                SerializedProperty end = property.GetEndProperty();
                SerializedProperty it = property.Copy();
                if (it.NextVisible(true) && !SerializedProperty.EqualContents(it, end))
                {
                    do
                    {
                        h += EditorGUI.GetPropertyHeight(it, true) + EditorGUIUtility.standardVerticalSpacing;
                    } while (it.NextVisible(false) && !SerializedProperty.EqualContents(it, end));
                }
            }
        }

        return h;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType != SerializedPropertyType.ManagedReference)
        {
            EditorGUI.PropertyField(position, property, label, true);
            EditorGUI.EndProperty();
            return;
        }

        float y = position.y;
        float x = position.x;
        float w = position.width;
        float lineH = EditorGUIUtility.singleLineHeight;

        Rect line = new Rect(x, y, w, lineH);
        line = EditorGUI.PrefixLabel(line, GUIUtility.GetControlID(FocusType.Passive), label);

        const float clearW = 36f;
        const float typeW = 88f;
        Rect clearR = new Rect(line.xMax - clearW, line.y, clearW, line.height);
        Rect typeR = new Rect(clearR.x - typeW - 2, line.y, typeW, line.height);
        Rect foldR = new Rect(line.x, line.y, typeR.x - 4 - line.x, line.height);

        string typeName = property.managedReferenceValue == null ? "未指定" : property.managedReferenceValue.GetType().Name;
        property.isExpanded = EditorGUI.Foldout(foldR, property.isExpanded, typeName, true);

        if (EditorGUI.DropdownButton(typeR, new GUIContent("类型…"), FocusType.Keyboard))
            ShowTypeMenu(property);

        if (GUI.Button(clearR, "清空"))
            AssignManaged(property, null);

        y += lineH + EditorGUIUtility.standardVerticalSpacing;

        if (property.isExpanded)
        {
            if (property.managedReferenceValue == null)
            {
                float boxH = EditorStyles.helpBox.CalcHeight(new GUIContent("点击「类型…」选择运动模式。"), w);
                EditorGUI.HelpBox(new Rect(x, y, w, boxH), "点击「类型…」选择运动模式。", MessageType.Info);
                y += boxH + EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                EditorGUI.indentLevel++;
                SerializedProperty end = property.GetEndProperty();
                SerializedProperty it = property.Copy();
                if (it.NextVisible(true) && !SerializedProperty.EqualContents(it, end))
                {
                    do
                    {
                        float fh = EditorGUI.GetPropertyHeight(it, true);
                        EditorGUI.PropertyField(new Rect(x, y, w, fh), it, true);
                        y += fh + EditorGUIUtility.standardVerticalSpacing;
                    } while (it.NextVisible(false) && !SerializedProperty.EqualContents(it, end));
                }
                EditorGUI.indentLevel--;
            }
        }

        EditorGUI.EndProperty();
    }

    static void ShowTypeMenu(SerializedProperty property)
    {
        var menu = new GenericMenu();
        foreach (var (path, type) in ConcreteTypes)
        {
            Type capturedType = type;
            bool on = type == null
                ? property.managedReferenceValue == null
                : property.managedReferenceValue != null && property.managedReferenceValue.GetType() == type;
            menu.AddItem(new GUIContent(path), on, () =>
            {
                object inst = capturedType == null ? null : Activator.CreateInstance(capturedType);
                AssignManaged(property, inst);
            });
        }
        menu.ShowAsContext();
    }

    static void AssignManaged(SerializedProperty property, object instance)
    {
        property.serializedObject.Update();
        property.managedReferenceValue = instance;
        property.isExpanded = instance != null;
        property.serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(property.serializedObject.targetObject);
    }
}
#endif
