using UnityEngine;
using UnityEngine.SceneManagement;

public class Object_Waypoint : MonoBehaviour
{
    [SerializeField] private string transferToScene;
    [Space]
    public RespawnType waypointType;
    [SerializeField] private RespawnType connectedWaypoint;
    [SerializeField] private bool canBeTriggered = true;

    private void OnValidate()
    {
        gameObject.name = "Object_Waypoint - " + waypointType.ToString() + " - " + transferToScene;

        if(waypointType == RespawnType.Enter)
        {
            connectedWaypoint = RespawnType.Exit;
        }
        else if(waypointType == RespawnType.Exit)   
        {
            connectedWaypoint = RespawnType.Enter;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!canBeTriggered) return;

        SaveManager.Instance.SaveGame();

        SceneManager.LoadScene(transferToScene);
    }

    private  void OnTriggerExit2D(Collider2D other)
    {
        canBeTriggered = true;
    }
}
