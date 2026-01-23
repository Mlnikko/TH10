using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy), true)]
public class EnemyConfiger : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("预览敌人配置文件", GUILayout.Height(30)))
        {
            Enemy editor = (Enemy)target;
            editor.LoadEnemyConfig();
        }

        if (GUILayout.Button("应用并保存当前配置", GUILayout.Height(30)))
        {
            Enemy editor = (Enemy)target;
            
        }

        GUILayout.EndHorizontal();
    }
}
