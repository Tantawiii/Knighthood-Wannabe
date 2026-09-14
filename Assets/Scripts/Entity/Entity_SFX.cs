using UnityEngine;

public class Entity_SFX : MonoBehaviour
{
    private AudioSource audioSource;

    [Header("SFX Names")]
    [SerializeField] private string attackHit;
    [SerializeField] private string attackMiss;
    [Space]
    [SerializeField] private float soundDistance = 15f;
    [SerializeField] private bool drawGizmos;


    private void Awake()
    {
        audioSource = GetComponentInChildren<AudioSource>();
    }

    public void PlayAttackHitSFX()
    {
        AudioManager.Instance.PlaySFX(attackHit, audioSource, soundDistance);
    }

    public void PlayAttackMissSFX()
    {
        AudioManager.Instance.PlaySFX(attackMiss, audioSource, soundDistance);
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, soundDistance);
    }
}
