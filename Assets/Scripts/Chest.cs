using UnityEngine;

public class Chest : MonoBehaviour, IDamagable
{
    Rigidbody2D rb => GetComponentInChildren<Rigidbody2D>();
    Animator animator => GetComponentInChildren<Animator>();
    Entity_VFX entityVFX => GetComponent<Entity_VFX>();

    [Header("Open Details")]
    [SerializeField] Vector2 openKnockback = new Vector2(0,3);

    public void TakeDamage(float damage, Transform damageDealer)
    {
        entityVFX.PlayOnDamageVFX();
        animator.SetBool("chestOpen", true);
        rb.linearVelocity = openKnockback;
        rb.angularVelocity = Random.Range(-200f, 200f);
    }
}
