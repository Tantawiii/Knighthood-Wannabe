using UnityEngine;

public class Object_Checkpoint : MonoBehaviour, ISaveable
{
    private Object_Checkpoint[] allCheckpoints;
    private Animator anim;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        allCheckpoints = FindObjectsByType<Object_Checkpoint>(FindObjectsSortMode.None);
    }

    public void ActivateCheckpoint(bool activate)
    {
        anim.SetBool("isActive", activate);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        foreach (var point in allCheckpoints)
        {
            point.ActivateCheckpoint(false);
        }

        SaveManager.Instance.GetGameData().savedCheckPoint = transform.position;

        ActivateCheckpoint(true);
    }

    public void LoadData(GameData data)
    {
        bool active = data.savedCheckPoint == transform.position;
        
        ActivateCheckpoint(active);

        if (active)
            Player.Instance.TeleportPlayer(transform.position);
    }

    public void SaveData(ref GameData data)
    {
    }
}
