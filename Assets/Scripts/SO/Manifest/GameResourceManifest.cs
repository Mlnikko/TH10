using System;
using System.Collections.Generic;
using UnityEngine;

public enum E_ResourceCategory
{
    Config,
    Prefab,
    Texture,
    Audio,
    Shader,
    Atlas,
}

[Serializable]
public class ResourceGroup
{
    public string groupName;
    public List<string> resourceIds = new();
}

[Serializable]
public class ResourceCategory
{
    [SerializeField] string categoryName;
    public E_ResourceCategory resCategory;
    public List<ResourceGroup> resGroups = new();
}

[CreateAssetMenu(fileName = "GameResourceManifest", menuName = "Configs/Manifest/GameResourceManifest")]
public class GameResourceManifest : GameConfig
{
    public List<ResourceCategory> resourceCategories = new();
}
