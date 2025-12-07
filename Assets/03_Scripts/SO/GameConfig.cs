using UnityEngine;
public class GameConfig : ScriptableObject
{
    public string ConfigId => name; // 默认用 asset 名作为 ID
}
