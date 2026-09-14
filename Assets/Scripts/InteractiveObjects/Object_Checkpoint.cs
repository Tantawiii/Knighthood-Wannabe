using System.Linq;
using UnityEngine;

public class Object_Checkpoint : MonoBehaviour, ISaveable
{
    [SerializeField] private string checkpointID;
    [SerializeField] private Transform respawnPoint;
    // private Object_Checkpoint[] allCheckpoints;
    public bool isActive {get; private set;}
    private Animator anim;
    private AudioSource fireAudioSource;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        fireAudioSource = GetComponent<AudioSource>();
        // allCheckpoints = FindObjectsByType<Object_Checkpoint>(FindObjectsSortMode.None);
    }

    private void OnValidate()
    {
        #if UNITY_EDITOR
        if (string.IsNullOrEmpty(checkpointID))
            checkpointID = System.Guid.NewGuid().ToString();
        #endif
    }

    public string GetCheckpointID() => checkpointID;

    public Vector3 GetPosition() => respawnPoint == null ? transform.position : respawnPoint.position;
    public void ActivateCheckpoint(bool activate)
    {
        isActive = activate;
        anim.SetBool("isActive", activate);
        if(isActive && !fireAudioSource.isPlaying)
        {
            fireAudioSource.Play();
        }
        
        if(!isActive)
        {
            fireAudioSource.Stop();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // foreach (var point in allCheckpoints)
        // {
        //     point.ActivateCheckpoint(false);
        // }

        // SaveManager.Instance.GetGameData().savedCheckPoint = transform.position;

        ActivateCheckpoint(true);
    }

    public void LoadData(GameData data)
    {
        // bool active = data.savedCheckPoint == transform.position;
        
        // ActivateCheckpoint(active);

        // if (active)
        //     Player.Instance.TeleportPlayer(transform.position);

        bool active = data.unlockedCheckpoints.TryGetValue(checkpointID, out active);
        ActivateCheckpoint(active);
    }

    public void SaveData(ref GameData data)
    {
        if (!isActive)
        {
            return;
        }

        if(!data.unlockedCheckpoints.ContainsKey(checkpointID))
        {
            data.unlockedCheckpoints.Add(checkpointID, true);
        }
    }
}
