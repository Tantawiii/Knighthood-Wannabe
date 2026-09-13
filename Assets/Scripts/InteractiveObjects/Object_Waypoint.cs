using UnityEngine;
using UnityEngine.SceneManagement;

public class Object_Waypoint : MonoBehaviour
{
    [SerializeField] private string transferToScene;
    [Space]
    [SerializeField] private RespawnType waypointType;
    [SerializeField] private RespawnType connectedWaypoint;
    [SerializeField] private Transform respawnPosition;
    [SerializeField] private bool canBeTriggered = true;

    public void SetCanBeTriggered(bool canBeTriggered) => this.canBeTriggered = canBeTriggered;

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

        GameManager.Instance.ChangeScene(transferToScene, connectedWaypoint);
    }

    private  void OnTriggerExit2D(Collider2D other)
    {
        canBeTriggered = true;
    }

    public RespawnType GetWaypointType() => waypointType;

    public Vector3 GetPositionAndSetTriggerToFalse()
    {
        canBeTriggered = false;
        return respawnPosition == null ? transform.position : respawnPosition.position;
    }
}
