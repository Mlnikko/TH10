using UnityEngine;

public interface IConfig
{
    bool Save(ScriptableObject SO);
    ScriptableObject Load();
}
