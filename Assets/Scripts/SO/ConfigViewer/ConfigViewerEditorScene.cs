#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 编辑器预览物体挂到 Viewer 所在场景/层级。
/// </summary>
public static class ConfigViewerEditorScene
{
    public static Transform EnsureChildRoot(Transform parent, string rootName, ref GameObject root)
    {
        if (root != null)
            return root.transform;

        root = new GameObject(rootName);
        root.hideFlags = HideFlags.DontSave;

        if (parent.gameObject.scene.IsValid())
            SceneManager.MoveGameObjectToScene(root, parent.gameObject.scene);

        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        return root.transform;
    }

    public static void AttachTransientObject(GameObject go, Transform parent, ref GameObject root, string rootName)
    {
        Transform rootTransform = EnsureChildRoot(parent, rootName, ref root);
        go.hideFlags = HideFlags.DontSave;

        if (parent.gameObject.scene.IsValid())
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);

        go.transform.SetParent(rootTransform, true);
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
