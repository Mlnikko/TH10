using System.Collections;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(FirstEnterTitle());
    }

    IEnumerator FirstEnterTitle()
    {
        BasePanel loadingPanel = UIManager.Instance.OpenPanel(E_Panel.Loading);
        yield return new WaitForSeconds(3);
        AudioManager.Instance.PlayAudio(AudioName.Title);
        loadingPanel.PanelFadeOut(1);
        yield return new WaitForSeconds(1);
        UIManager.Instance.OpenPanel(E_Panel.Title);
    }
}
