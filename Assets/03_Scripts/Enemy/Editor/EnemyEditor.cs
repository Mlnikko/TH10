using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Enemy), true)]
public class EnemyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("读取敌人配置文件", GUILayout.Height(30)))
        {
            Enemy editor = (Enemy)target;
            editor.LoadEnemyConfig();
        }

        if (GUILayout.Button("应用并保存当前配置", GUILayout.Height(30)))
        {
            Enemy editor = (Enemy)target;
            editor.SaveEnemyConfig();
        }

        GUILayout.EndHorizontal();
    }
}
