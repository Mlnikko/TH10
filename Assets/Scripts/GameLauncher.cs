using System.Collections;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    public GameObject IngameDebugPanel;
    void Awake()
    {
        _ = UIManager.Instance;

        if (IngameDebugPanel != null)
        {
            Instantiate(IngameDebugPanel);
        }
    }

    async void Start()
    {
        await ResManager.Instance.InitializeAsync();
        await GameResDB.Instance.InitializeAsync();

        bool sceneLoaded = await SceneLoader.LoadSceneAsync("TitleScene");

        if (sceneLoaded)
        {
            try
            {
                var panel = await UIManager.Instance.ShowPanelAsync<MenuPanel>();
                if (panel == null)
                {
                    Logger.Error("MenuPanel failed to load or instantiate.");
                }
            }
            catch (System.Exception ex)
            {
                Logger.Critical(ex.ToString());
            }
        }
        else
        {
            Logger.Error("Failed to load TitleScene!");
        }
    }
}
