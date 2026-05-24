#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponConfig))]
[CanEditMultipleObjects]
public class WeaponConfigAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "danmakuEmitterConfigIds");

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
