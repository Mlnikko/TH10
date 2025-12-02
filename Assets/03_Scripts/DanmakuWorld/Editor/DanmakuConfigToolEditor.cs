using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuConfiger), true)]
public class DanmakuConfigToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("º”‘ÿµØƒª≈‰÷√", GUILayout.Height(30)))
        {
            DanmakuConfiger editor = (DanmakuConfiger)target;
            editor.LoadDanmakuConfig();
        }

        if (GUILayout.Button("‘§¿¿µØƒª±Ìœ÷", GUILayout.Height(30)))
        {
            DanmakuConfiger editor = (DanmakuConfiger)target;
            editor.PreviewDanmaku();
        }

        // 3. ÃÌº”±£¥Ê∞¥≈•
        if (GUILayout.Button("±£¥ÊµØƒª≈‰÷√", GUILayout.Height(30)))
        {
            DanmakuConfiger editor = (DanmakuConfiger)target;
            editor.SaveDanmakuConfig();
        }

        GUILayout.EndHorizontal();
    }
}
