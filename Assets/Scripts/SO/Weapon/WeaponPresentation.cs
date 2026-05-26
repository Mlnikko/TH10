using UnityEngine;

/// <summary>
/// 池化武器根节点出池时的复位（武器预制体无 Sprite，表现由子级发射器实例承担）。
/// </summary>
public static class WeaponPresentation
{
    public static void Apply(WeaponConfig config, GameObject root)
    {
        if (root == null)
            return;

        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
    }
}
