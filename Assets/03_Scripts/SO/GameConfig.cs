using UnityEngine;
public abstract class GameConfig : ScriptableObject
{
    [SerializeField] protected string configId = string.Empty;
    public virtual string ConfigId => configId;
}
