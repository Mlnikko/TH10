using UnityEngine;
using UnityEngine.SceneManagement;

public struct BattleConfig
{
    public E_Rank rank;
    public E_CharacterName character;
    public E_Weapon weapon;
}


public class BattleManager : SingletonMono<BattleManager>
{
    public static BattleConfig battleConfig;

    protected override void OnSingletonInit()
    {
        EventManager.Instance.RegistEvent(E_Event.BattleStart, StartBattle);
    }

    public void StartBattle()
    {
        Debug.Log("Battle Start!");
        SceneManager.LoadScene("BattleScene");
        SceneManager.UnloadSceneAsync("TitleScene");
    }

    public void CreatePlayer()
    {

    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        EventManager.Instance.UnRegistEvent(E_Event.BattleStart, StartBattle);
    }
}
