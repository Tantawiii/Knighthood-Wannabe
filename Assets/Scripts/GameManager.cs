using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private Vector3 lastDeathPosition;

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

    public void SetLastDeathPosition(Vector3 position) => lastDeathPosition = position;

    public void RestartScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        ChangeScene(sceneName, RespawnType.NoneSpecific);
    }

    public void ChangeScene(string sceneName, RespawnType respawnType)
    {
        SaveManager.Instance.SaveGame();
        StartCoroutine(ChangeSceneCo(sceneName, respawnType));
    }

    private IEnumerator ChangeSceneCo(string sceneName, RespawnType respawnType)
    {
        // Fade Effect

        yield return new WaitForSeconds(1f);

        SceneManager.LoadScene(sceneName);

        yield return new WaitForSeconds(.2f);

        Vector3 respawnPosition = GetNewPlayerPostion(respawnType);

        if(respawnPosition != Vector3.zero)
        {
            Player.Instance.TeleportPlayer(respawnPosition);
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
            .OrderBy(pos => Vector3.Distance(pos, lastDeathPosition)) // Order the positions by distance to the last death position.
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
}
