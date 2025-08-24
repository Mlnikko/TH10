using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterPrefab))]
public class CharacterPrefabEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("读取角色配置文件", GUILayout.Height(30)))
        {
            CharacterPrefab editor = (CharacterPrefab)target;
            editor.LoadCharacterConfig();
        }

        if (GUILayout.Button("应用并保存当前配置", GUILayout.Height(30)))
        {
            CharacterPrefab editor = (CharacterPrefab)target;
            editor.SaveCharacterConfig();
        }

        GUILayout.EndHorizontal();
    }
}
