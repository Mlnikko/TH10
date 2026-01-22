using UnityEngine;
public abstract class GameConfig : ScriptableObject
{
    public virtual string ConfigId => name;

    public virtual string AddressableKeyPrefix => ConfigHelper.CONFIG_PREFIX;
}
