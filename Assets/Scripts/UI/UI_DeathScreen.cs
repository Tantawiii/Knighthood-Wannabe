using UnityEngine;

public class UI_DeathScreen : MonoBehaviour
{
    public void GoToTown()
    {
        GameManager.Instance.ChangeScene("Level 0", RespawnType.NoneSpecific);
    }

    public void LastCheckpoint()
    {
        GameManager.Instance.RestartScene();
    }

    public void MainMenu() => GameManager.Instance.ChangeScene("MainMenu", RespawnType.NoneSpecific);
}
