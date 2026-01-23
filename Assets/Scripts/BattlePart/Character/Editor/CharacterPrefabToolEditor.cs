using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterPrefabTool))]
public class CharacterPrefabToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("读取角色配置文件", GUILayout.Height(30)))
        {
            CharacterPrefabTool configer = (CharacterPrefabTool)target;
            if (configer.CharacterConfig == null)
            {
                Logger.Warn("未指定 CharacterConfig！");
                return;
            }
            configer.LoadCharacterConfig();
            Logger.Debug($"已读取角色配置: {configer.CharacterConfig.name}");
        }

        if (GUILayout.Button("应用并保存当前配置", GUILayout.Height(30)))
        {
            CharacterPrefabTool configer = (CharacterPrefabTool)target;
            if (configer.CharacterConfig == null)
            {
                Logger.Warn("未指定 CharacterConfig！");
                return;
            }
            configer.SaveCharacterConfig();
            EditorUtility.SetDirty(configer.CharacterConfig);
            AssetDatabase.SaveAssets();
            Logger.Debug($"已保存角色配置: {configer.CharacterConfig.name}");
        }

        GUILayout.EndHorizontal();
    }
}
