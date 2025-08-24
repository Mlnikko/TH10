using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemyPrefab), true)]
public class EnemyPrefabEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("读取敌人配置文件", GUILayout.Height(30)))
        {
            EnemyPrefab editor = (EnemyPrefab)target;
            editor.LoadEnemyConfig();
        }

        if (GUILayout.Button("应用并保存当前配置", GUILayout.Height(30)))
        {
            EnemyPrefab editor = (EnemyPrefab)target;
            editor.SaveEnemyConfig();
        }

        GUILayout.EndHorizontal();
    }
}
