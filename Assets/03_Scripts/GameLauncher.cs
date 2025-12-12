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

    // 改为 async void —— 这是 Unity 中启动异步逻辑的标准方式
    async void Start()
    {
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
                Logger.Exception(ex);
            }
        }
        else
        {
            Logger.Error("Failed to load TitleScene!");
        }
    }
}
