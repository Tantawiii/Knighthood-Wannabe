using UnityEngine;
using UnityEngine.SceneManagement;

public class Object_Portal : MonoBehaviour
{
    public static Object_Portal Instance;

    public bool isActive {get; private set;}
    [SerializeField] private Vector2 defaultPosition; // Default position appears in town
    [SerializeField] private string townSceneName = "Level_0"; // Scene name of the town

    [SerializeField] private Transform respawnPoint; // Position where the player will respawn when entering the portal
    [SerializeField] private bool canBeTriggered;   

    private string currentSceneName;
    private string lastSceneName; // will be used to returning destination.

    private void Awake()
    {
        Instance = this;
        currentSceneName = SceneManager.GetActiveScene().name;
        transform.position = new Vector3(9999,9999);
    }

    public void ActivatePortal()
    {
        
    }

    private void UseTeleport()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!canBeTriggered) return;

        UseTeleport();
    }
    private void OnTriggerExit2D(Collider2D collision) => canBeTriggered = true;

    public void SetCanBeTriggered(bool canBeTriggered) => this.canBeTriggered = canBeTriggered;
    public Vector3 GetPosition() => respawnPoint != null ? respawnPoint.position : transform.position;
}
