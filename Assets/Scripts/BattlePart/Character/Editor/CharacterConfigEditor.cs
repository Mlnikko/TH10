using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterConfigViewer))]
public class CharacterConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("读取并预览角色配置", GUILayout.Height(30)))
        {
            CharacterConfigViewer viewer = (CharacterConfigViewer)target;
            if (viewer.CharacterConfig == null)
            {
                Logger.Warn("未指定 CharacterConfig！");
                return;
            }
            viewer.LoadCharacterConfig();
            Logger.Debug($"已读取角色配置: {viewer.CharacterConfig.name}");
        }

        if (GUILayout.Button("应用并保存角色配置", GUILayout.Height(30)))
        {
            CharacterConfigViewer viewer = (CharacterConfigViewer)target;
            if (viewer.CharacterConfig == null)
            {
                Logger.Warn("未指定 CharacterConfig！");
                return;
            }

            if (EditorUtility.DisplayDialog(
            "确认保存？",
            "将覆盖资产",
            "确定", "取消"))
            {
                viewer.SaveCharacterConfig();
                EditorUtility.SetDirty(viewer.CharacterConfig);
                AssetDatabase.SaveAssets();
                Logger.Debug($"已保存角色配置: {viewer.CharacterConfig.name}");
            }
        }

        GUILayout.EndHorizontal();
    }
}
