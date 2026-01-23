using UnityEngine;
public abstract class GameConfig : ScriptableObject
{
    public virtual string ConfigId => name;
}
