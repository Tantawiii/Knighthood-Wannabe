using UnityEngine;
using UnityEngine.SceneManagement;

public class Object_Portal : MonoBehaviour, ISaveable
{
    public static Object_Portal Instance;

    public bool isActive {get; private set;}
    [SerializeField] private Vector2 defaultPosition; // Default position appears in town
    [SerializeField] private string townSceneName = "Level_0"; // Scene name of the town

    [SerializeField] private Transform respawnPoint; // Position where the player will respawn when entering the portal
    [SerializeField] private bool canBeTriggered;   

    private string currentSceneName;
    private string returnSceneName;
    private bool returningFromTown;

    private void Awake()
    {
        Instance = this;
        currentSceneName = SceneManager.GetActiveScene().name;
        transform.position = new Vector3(9999,9999);
    }

    public void ActivatePortal(Vector3 position, int facingDir = 1)
    {
        isActive = true;
        transform.position = position;
        SaveManager.Instance.GetGameData().inScenePortals.Clear();

        if(facingDir == -1)
            transform.Rotate(0, 180, 0);
    }

    public void DisableIfNeeded()
    {
        if(!returningFromTown)
        {
            return;
        }

        SaveManager.Instance.GetGameData().inScenePortals.Remove(currentSceneName);
        isActive = false;
        transform.position = new Vector3(9999,9999);
    }

    private void UseTeleport()
    {
        string destinationScene = IsInTownScene() ? returnSceneName : townSceneName;

        GameManager.Instance.ChangeScene(destinationScene, RespawnType.Portal);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!canBeTriggered) return;

        UseTeleport();
    }
    private void OnTriggerExit2D(Collider2D collision) => canBeTriggered = true;

    public void SetCanBeTriggered(bool canBeTriggered) => this.canBeTriggered = canBeTriggered;
    public Vector3 GetPosition() => respawnPoint != null ? respawnPoint.position : transform.position;

    private bool IsInTownScene() => currentSceneName == townSceneName;

    public void LoadData(GameData data)
    {
        if(IsInTownScene() && data.inScenePortals.Count > 0)
        {
            transform.position = defaultPosition;
            isActive = true;
        }
        else if(data.inScenePortals.TryGetValue(currentSceneName, out Vector3 portalPosition))
        {
            transform.position = portalPosition;
            isActive = true;
        }

        returningFromTown = data.returningFromTown;
        returnSceneName = data.portalDestinationSceneName;
    }

    public void SaveData(ref GameData data)
    {
        data.returningFromTown = IsInTownScene();
        
        if(isActive && !IsInTownScene())
        {
            data.inScenePortals[currentSceneName] = transform.position;
            data.portalDestinationSceneName = currentSceneName;
        }
        else
        {
            data.inScenePortals.Remove(currentSceneName);
        }

    }
}
