#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 编辑器预览物体挂到 Viewer 所在场景/层级。
/// </summary>
public static class ConfigViewerEditorScene
{
    /// <summary>
    /// 是否允许将 DontSave 预览物挂到 anchor 下（Prefab 隔离模式或场景实例）。
    /// 禁止挂到 Project 中的 Prefab 资产本体，否则会触发 SetParent 保护报错。
    /// </summary>
    public static bool CanHostTransientPreview(Transform anchor)
    {
        if (anchor == null)
            return false;

        var go = anchor.gameObject;

        if (PrefabStageUtility.GetPrefabStage(go) != null)
            return true;

        if (!go.scene.IsValid())
            return false;

        return !PrefabUtility.IsPartOfPrefabAsset(go);
    }

    public static Transform EnsureChildRoot(Transform parent, string rootName, ref GameObject root)
    {
        if (!CanHostTransientPreview(parent))
        {
            DestroyRoot(ref root);
            return null;
        }

        if (root != null)
            return root.transform;

        root = new GameObject(rootName);
        root.hideFlags = HideFlags.DontSave;

        SceneManager.MoveGameObjectToScene(root, parent.gameObject.scene);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root.transform;
    }

    public static bool AttachTransientObject(GameObject go, Transform parent, ref GameObject root, string rootName)
    {
        if (go == null)
            return false;

        Transform rootTransform = EnsureChildRoot(parent, rootName, ref root);
        if (rootTransform == null)
        {
            Object.DestroyImmediate(go);
            return false;
        }

        go.hideFlags = HideFlags.DontSave;
        SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
        go.transform.SetParent(rootTransform, true);
        return true;
    }

    public static void DestroyRoot(ref GameObject root)
    {
        if (root == null)
            return;

        Object.DestroyImmediate(root);
        root = null;
    }
}
#endif
