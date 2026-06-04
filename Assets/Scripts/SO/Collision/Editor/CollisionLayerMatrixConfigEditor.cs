#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CollisionLayerMatrixConfig))]
public class CollisionLayerMatrixConfigEditor : Editor
{
    const float RowLabelWidth = 108f;
    const float CellWidth = 22f;

    public override void OnInspectorGUI()
    {
        var config = (CollisionLayerMatrixConfig)target;
        config.EnsureArraySizes();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("碰撞层矩阵", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "勾选表示两层之间会做碰撞检测（对称）。下方「发起粗测」决定该层是否主动查询网格。",
            MessageType.None);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("应用默认 STG 矩阵", GUILayout.Height(24)))
        {
            Undo.RecordObject(config, "Apply Default Collision Matrix");
            CollisionLayerMatrixDefaults.ApplyGameplayDefaults(config);
            EditorUtility.SetDirty(config);
        }

        if (GUILayout.Button("清空全部碰撞", GUILayout.Height(24)))
        {
            Undo.RecordObject(config, "Clear Collision Matrix");
            config.ClearAllPairs(false);
            EditorUtility.SetDirty(config);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        DrawPairMatrix(config);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("发起网格粗测", EditorStyles.boldLabel);
        DrawInitiatesBroadphase(config);

        if (GUI.changed)
            EditorUtility.SetDirty(config);
    }

    static void DrawPairMatrix(CollisionLayerMatrixConfig config)
    {
        int n = ColliderLayerDefinitions.LayerCount;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(RowLabelWidth);
        for (int col = 0; col < n; col++)
        {
            var label = new GUIContent(ColliderLayerDefinitions.DisplayNames[col]);
            EditorGUILayout.LabelField(label, GUILayout.Width(CellWidth));
        }
        EditorGUILayout.EndHorizontal();

        for (int row = 0; row < n; row++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(ColliderLayerDefinitions.DisplayNames[row], GUILayout.Width(RowLabelWidth));

            for (int col = 0; col < n; col++)
            {
                bool current = config.GetPair(row, col);
                EditorGUI.BeginChangeCheck();
                bool next = EditorGUILayout.Toggle(current, GUILayout.Width(CellWidth));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Edit Collision Layer Matrix");
                    config.SetPairSymmetric(row, col, next);
                }
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    static void DrawInitiatesBroadphase(CollisionLayerMatrixConfig config)
    {
        for (int i = 0; i < ColliderLayerDefinitions.LayerCount; i++)
        {
            var layer = ColliderLayerDefinitions.FromIndex(i);
            bool current = config.GetInitiatesBroadphase(layer);
            EditorGUI.BeginChangeCheck();
            bool next = EditorGUILayout.ToggleLeft(ColliderLayerDefinitions.DisplayNames[i], current);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(config, "Edit Broadphase Initiator");
                config.SetInitiatesBroadphase(layer, next);
            }
        }
    }
}
#endif
