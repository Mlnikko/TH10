using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DropItemConfigViewer), true)]
public class DropItemConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("预览掉落物表现", GUILayout.Height(30)))
        {
            var viewer = (DropItemConfigViewer)target;
            viewer.PreviewDropItem();
        }

        if (GUILayout.Button("保存当前配置", GUILayout.Height(30)))
        {
            var viewer = (DropItemConfigViewer)target;

            if (EditorUtility.DisplayDialog("确认保存？", "将覆盖资产", "确定", "取消"))
            {
                viewer.SaveDropItemConfig();
                EditorUtility.SetDirty(viewer.dropItemConfig);
                AssetDatabase.SaveAssets();
            }
        }

        GUILayout.EndHorizontal();
    }
}
