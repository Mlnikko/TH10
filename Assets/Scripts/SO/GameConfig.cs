using UnityEngine;
using System;
public abstract class GameConfig : ScriptableObject
{
    [HideInInspector]
    public string configId = string.Empty;

    [NonSerialized]
    public int configIndex = -1;
}
