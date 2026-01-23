using System;
using UnityEngine;

public static class PrefabDB
{
    private static GameObject[] _danmakuPrefabs;

    internal static void SetBulletPrefabs(GameObject[] prefabs) => _danmakuPrefabs = prefabs ?? Array.Empty<GameObject>();

    public static GameObject GetBulletPrefab(int index) =>
        (uint)index < (uint)_danmakuPrefabs.Length ? _danmakuPrefabs[index] : null;
}