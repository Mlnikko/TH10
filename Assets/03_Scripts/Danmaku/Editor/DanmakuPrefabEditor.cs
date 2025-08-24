using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuPrefab), true)]
public class DanmakuPrefabEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("º”‘ÿµØƒª≈‰÷√", GUILayout.Height(30)))
        {
            DanmakuPrefab editor = (DanmakuPrefab)target;
            editor.LoadDanmakuConfig();
        }

        if (GUILayout.Button("‘§¿¿µØƒª±Ìœ÷", GUILayout.Height(30)))
        {
            DanmakuPrefab editor = (DanmakuPrefab)target;
            editor.PreviewDanmaku();
        }

        // 3. ÃÌº”±£¥Ê∞¥≈•
        if (GUILayout.Button("±£¥ÊµØƒª≈‰÷√", GUILayout.Height(30)))
        {
            DanmakuPrefab editor = (DanmakuPrefab)target;
            editor.SaveDanmakuConfig();
        }

        GUILayout.EndHorizontal();
    }
}
