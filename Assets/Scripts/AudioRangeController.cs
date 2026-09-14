using UnityEngine;

public class AudioRangeController : MonoBehaviour
{
    private AudioSource source;
    private Transform player;

    [SerializeField] private float minDistanceToHearSound = 15f;
    [SerializeField] private bool drawGizmos;
    private float maxVolume;


    private void Start()
    {
        player = Player.Instance.transform;
        source = GetComponent<AudioSource>();

        maxVolume = source.volume; // Store the original volume
    }

    private void Update()
    {
        if(player == null)
        {
            return;
        }

        float distance = Vector2.Distance(player.position, transform.position);
        float t = Mathf.Clamp01(1 - (distance / minDistanceToHearSound));
        
        float targetVolume = Mathf.Lerp(0, maxVolume, t * t); // Adjust volume based on distance, with exponential falloff for smoother transition

        source.volume = Mathf.Lerp(source.volume, targetVolume, Time.deltaTime * 3f); // Smoothly interpolate to the target volume
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minDistanceToHearSound);
    }
}
