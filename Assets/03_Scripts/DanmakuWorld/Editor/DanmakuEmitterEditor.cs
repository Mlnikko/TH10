using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DanmakuEmitter), true)]
public class DanmakuEmitterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("读取发射器配置文件", GUILayout.Height(30)))
        {
            DanmakuEmitter editor = (DanmakuEmitter)target;
            editor.LoadEmitterConfig();
        }

        if (GUILayout.Button("预览发射效果", GUILayout.Height(30)))
        {
            DanmakuEmitter editor = (DanmakuEmitter)target;
            //editor.PreviewEmitterEffect();
        }

        if (GUILayout.Button("应用并保存当前配置", GUILayout.Height(30)))
        {
            DanmakuEmitter editor = (DanmakuEmitter)target;
            //editor.SaveEmitterConfig();
        }

        GUILayout.EndHorizontal();
    }
}
