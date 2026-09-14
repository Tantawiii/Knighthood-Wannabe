using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour, ISaveable
{
    public static GameManager Instance;
    private Vector3 lastPlayerPosition;

    public string lastScenePlayed;
    private bool dataLoaded;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // public void SetLastPlayerPosition(Vector3 position) => lastPlayerPosition = position;    

    public void ContinuePlay()
    {
        ChangeScene(lastScenePlayed, RespawnType.NoneSpecific);
    }

    public void RestartScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        ChangeScene(sceneName, RespawnType.NoneSpecific);
    }

    public void ChangeScene(string sceneName, RespawnType respawnType)
    {
        // Time.timeScale = 1f; // Reset time scale to normal speed
        SaveManager.Instance.SaveGame();
        StartCoroutine(ChangeSceneCo(sceneName, respawnType));
    }

    private IEnumerator ChangeSceneCo(string sceneName, RespawnType respawnType)
    {
        UI_FadeScreen fadeScreenUI = FindFadeScreenUI();

        fadeScreenUI?.FadeOut(); // Fade to black over 1 second

        yield return fadeScreenUI?.fadeEffectCo; // Wait for the fade effect to complete

        SceneManager.LoadScene(sceneName);

        dataLoaded = false; // Reset the flag before loading data
        yield return null; // Wait for one frame to ensure the scene is fully loaded

        while(dataLoaded == false)
        {
            yield return null; // Wait until data is loaded
        }

        fadeScreenUI = FindFadeScreenUI();

        fadeScreenUI?.FadeIn(); // Fade to transparent over 1 second

        // yield return dataLoaded ? null : new WaitUntil(() => dataLoaded); // Wait until data is loaded

        Player player = Player.Instance;

        if(player == null)
        {
            yield break; // Exit the coroutine if the player is not found
        }

        Vector3 respawnPosition = GetNewPlayerPostion(respawnType);

        if(respawnPosition != Vector3.zero)
        {
            player.TeleportPlayer(respawnPosition);
        }
    }

    private UI_FadeScreen FindFadeScreenUI()
    {
        if(UI.Instance != null)
        {
            return UI.Instance.fadeUI;
        }
        else
        {
            return FindFirstObjectByType<UI_FadeScreen>();
        }
    }

    private Vector3 GetNewPlayerPostion(RespawnType type)
    {
        if(type == RespawnType.Portal)
        {
            Object_Portal portal = Object_Portal.Instance;

            Vector3 portalPosition = portal.GetPosition();

            portal.SetCanBeTriggered(false);
            portal.DisableIfNeeded();

            return portalPosition;
        }

        if(type == RespawnType.NoneSpecific)
        {
            var data = SaveManager.Instance.GetGameData();
            var checkpoints = FindObjectsByType<Object_Checkpoint>(FindObjectsSortMode.None);
            var unlockedCheckpoints = checkpoints
                .Where(cp => data.unlockedCheckpoints.TryGetValue(cp.GetCheckpointID(), out bool unlocked) && unlocked)
                .Select(cp => cp.GetPosition())
                .ToList();

            var enterWaypoints = FindObjectsByType<Object_Waypoint>(FindObjectsSortMode.None)
                .Where(wp => wp.GetWaypointType() == RespawnType.Enter)
                .Select(wp => wp.GetPositionAndSetTriggerToFalse())
                .ToList();

            var selectedPositions = unlockedCheckpoints.Concat(enterWaypoints).ToList(); // Combine both lists into one list.

            if(selectedPositions.Count == 0)
            {
                return Vector3.zero; // No valid positions found.
            }

            return selectedPositions
            .OrderBy(pos => Vector3.Distance(pos, lastPlayerPosition)) // Order the positions by distance to the last death position.
            .First();  // Return the closest position.
        }

        return GetWaypointPosition(type);
    }

    private Vector3 GetWaypointPosition(RespawnType type)
    {
        var waypoints = FindObjectsByType<Object_Waypoint>(FindObjectsSortMode.None);

        foreach (var waypoint in waypoints)
        {
            if (waypoint.GetWaypointType() == type)
            {
                return waypoint.GetPositionAndSetTriggerToFalse();
            }
        }

        return Vector3.zero;
    }

    public void LoadData(GameData data)
    {
        lastScenePlayed = data.lastScenePlayed;
        lastPlayerPosition = data.lastPlayerPosition;

        if(string.IsNullOrEmpty(lastScenePlayed))
        {
            lastScenePlayed = "Level 0";
        }

        dataLoaded = true; // Set the flag to indicate that data has been loaded
    }

    public void SaveData(ref GameData data)
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if(currentSceneName == "MainMenu")
        {
            return; // Do not save the main menu scene
        }

        data.lastPlayerPosition = Player.Instance.transform.position;

        data.lastScenePlayed = currentSceneName;

        dataLoaded = false; // Reset the flag after saving data
    }
}
