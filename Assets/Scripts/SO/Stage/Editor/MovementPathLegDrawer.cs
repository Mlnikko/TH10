#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// 路径段 Inspector：按 <see cref="E_PathSegmentCurve"/> 仅显示对应曲线参数。
/// </summary>
[CustomPropertyDrawer(typeof(MovementPathLeg))]
public sealed class MovementPathLegDrawer : PropertyDrawer
{
    const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        return EditorGUIUtility.singleLineHeight
               + Spacing
               + SumVisibleFieldHeights(property);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (!property.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }

        float y = position.y + EditorGUIUtility.singleLineHeight + Spacing;
        float lineWidth = position.width;
        float indent = EditorGUI.IndentedRect(new Rect(position.x, y, lineWidth, 1f)).x - position.x;

        y = DrawCurveField(property, position.x + indent, y, lineWidth - indent);
        y = DrawField(property, nameof(MovementPathLeg.travelSeconds), position.x + indent, y, lineWidth - indent);

        switch (GetCurve(property))
        {
            case E_PathSegmentCurve.Arc:
                DrawField(property, nameof(MovementPathLeg.arcBulge), position.x + indent, y, lineWidth - indent);
                break;
            case E_PathSegmentCurve.Bezier:
                y = DrawField(property, nameof(MovementPathLeg.bezierHandle1Local), position.x + indent, y, lineWidth - indent);
                DrawField(property, nameof(MovementPathLeg.bezierHandle2Local), position.x + indent, y, lineWidth - indent);
                break;
            case E_PathSegmentCurve.Sine:
                y = DrawField(property, nameof(MovementPathLeg.sineAmplitude), position.x + indent, y, lineWidth - indent);
                y = DrawField(property, nameof(MovementPathLeg.sineHz), position.x + indent, y, lineWidth - indent);
                DrawField(property, nameof(MovementPathLeg.sinePhaseRad), position.x + indent, y, lineWidth - indent);
                break;
        }

        EditorGUI.EndProperty();
    }

    static float SumVisibleFieldHeights(SerializedProperty property)
    {
        float h = FieldHeight(property, nameof(MovementPathLeg.curve))
                  + FieldHeight(property, nameof(MovementPathLeg.travelSeconds));

        switch (GetCurve(property))
        {
            case E_PathSegmentCurve.Arc:
                h += FieldHeight(property, nameof(MovementPathLeg.arcBulge));
                break;
            case E_PathSegmentCurve.Bezier:
                h += FieldHeight(property, nameof(MovementPathLeg.bezierHandle1Local))
                      + FieldHeight(property, nameof(MovementPathLeg.bezierHandle2Local));
                break;
            case E_PathSegmentCurve.Sine:
                h += FieldHeight(property, nameof(MovementPathLeg.sineAmplitude))
                      + FieldHeight(property, nameof(MovementPathLeg.sineHz))
                      + FieldHeight(property, nameof(MovementPathLeg.sinePhaseRad));
                break;
        }

        return h + Spacing * Mathf.Max(0, GetVisibleFieldCount(GetCurve(property)) - 1);
    }

    static int GetVisibleFieldCount(E_PathSegmentCurve curve) => curve switch
    {
        E_PathSegmentCurve.Arc => 3,
        E_PathSegmentCurve.Bezier => 4,
        E_PathSegmentCurve.Sine => 5,
        _ => 2
    };

    static float DrawField(SerializedProperty parent, string relativeName, float x, float y, float width)
    {
        var prop = parent.FindPropertyRelative(relativeName);
        if (prop == null)
            return y;

        float h = EditorGUI.GetPropertyHeight(prop, true);
        EditorGUI.PropertyField(new Rect(x, y, width, h), prop, true);
        return y + h + Spacing;
    }

    static float FieldHeight(SerializedProperty parent, string relativeName)
    {
        var prop = parent.FindPropertyRelative(relativeName);
        return prop != null ? EditorGUI.GetPropertyHeight(prop, true) : 0f;
    }

    static E_PathSegmentCurve GetCurve(SerializedProperty property)
    {
        var curveProp = property.FindPropertyRelative(nameof(MovementPathLeg.curve));
        return curveProp != null
            ? (E_PathSegmentCurve)curveProp.enumValueIndex
            : E_PathSegmentCurve.Linear;
    }

    static float DrawCurveField(SerializedProperty parent, float x, float y, float width)
    {
        var curveProp = parent.FindPropertyRelative(nameof(MovementPathLeg.curve));
        if (curveProp == null)
            return y;

        float h = EditorGUI.GetPropertyHeight(curveProp, true);
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(new Rect(x, y, width, h), curveProp, true);
        if (EditorGUI.EndChangeCheck())
            ApplyDefaultsFromSerialized(parent, (E_PathSegmentCurve)curveProp.enumValueIndex);

        return y + h + Spacing;
    }

    static void ApplyDefaultsFromSerialized(SerializedProperty legProp, E_PathSegmentCurve curve)
    {
        TryGetSegmentEndpoints(legProp, out Vector2 from, out Vector2 to);

        var leg = new MovementPathLeg();
        PathMovementLegDefaults.Apply(leg, curve, from, to);

        SetFloat(legProp, nameof(MovementPathLeg.travelSeconds), leg.travelSeconds);
        SetFloat(legProp, nameof(MovementPathLeg.arcBulge), leg.arcBulge);
        SetVector2(legProp, nameof(MovementPathLeg.bezierHandle1Local), leg.bezierHandle1Local);
        SetVector2(legProp, nameof(MovementPathLeg.bezierHandle2Local), leg.bezierHandle2Local);
        SetFloat(legProp, nameof(MovementPathLeg.sineAmplitude), leg.sineAmplitude);
        SetFloat(legProp, nameof(MovementPathLeg.sineHz), leg.sineHz);
        SetFloat(legProp, nameof(MovementPathLeg.sinePhaseRad), leg.sinePhaseRad);
    }

    static bool TryGetSegmentEndpoints(SerializedProperty legProp, out Vector2 from, out Vector2 to)
    {
        from = Vector2.zero;
        to = PathMovementLegDefaults.FallbackSegmentEnd;

        string path = legProp.propertyPath;
        int legsToken = path.LastIndexOf(".legs", System.StringComparison.Ordinal);
        if (legsToken < 0)
            return false;

        int legIndex = ParseLegArrayIndex(path);
        if (legIndex < 0)
            return false;

        var routeProp = legProp.serializedObject.FindProperty(path.Substring(0, legsToken));
        var nodesProp = routeProp?.FindPropertyRelative(nameof(PathRouteMovementData.nodes));
        if (nodesProp == null || !nodesProp.isArray || legIndex >= nodesProp.arraySize)
            return false;

        to = nodesProp.GetArrayElementAtIndex(legIndex).FindPropertyRelative(nameof(MovementPathNode.positionLocal)).vector2Value;
        if (legIndex > 0)
        {
            from = nodesProp.GetArrayElementAtIndex(legIndex - 1)
                .FindPropertyRelative(nameof(MovementPathNode.positionLocal)).vector2Value;
        }

        return true;
    }

    static int ParseLegArrayIndex(string propertyPath)
    {
        const string token = ".Array.data[";
        int start = propertyPath.LastIndexOf(token, System.StringComparison.Ordinal);
        if (start < 0)
            return -1;

        start += token.Length;
        int end = propertyPath.IndexOf(']', start);
        if (end < 0)
            return -1;

        return int.TryParse(propertyPath.Substring(start, end - start), out int index) ? index : -1;
    }

    static void SetFloat(SerializedProperty parent, string name, float value)
    {
        var prop = parent.FindPropertyRelative(name);
        if (prop != null)
            prop.floatValue = value;
    }

    static void SetVector2(SerializedProperty parent, string name, Vector2 value)
    {
        var prop = parent.FindPropertyRelative(name);
        if (prop != null)
            prop.vector2Value = value;
    }
}
#endif
