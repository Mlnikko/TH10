using UnityEngine;
public abstract class GameConfig : ScriptableObject
{
    [Header("ÅäÖÃÎÄ¼þID")]
    public string ConfigId = string.Empty;

    public virtual string AddressableKeyPrefix => ConfigHelper.CONFIG_PREFIX;
}
