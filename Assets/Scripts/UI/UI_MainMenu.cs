using UnityEngine;

public class UI_MainMenu : MonoBehaviour
{
    private void Start()
    {
        transform.root.GetComponentInChildren<UI_FadeScreen>().FadeIn(); // Fade to transparent over 1 second
    }

    public void PlayGame()
    {
        GameManager.Instance.ContinuePlay();
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
