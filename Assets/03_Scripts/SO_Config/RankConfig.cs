using System;
using UnityEngine;

public enum E_Rank
{
    Eazy,
    Normal,
    Hard,
    Lunatic,
    Extra
}

[Serializable]
public class Rank
{
    public E_Rank rank;
    [TextArea(1, 5)]
    public string Description;
}

[CreateAssetMenu(fileName = "NewRankConfig", menuName = "Custom/RankConfig")]
public class RankConfig : GameConfig
{
    public Rank[] ranks;
}
