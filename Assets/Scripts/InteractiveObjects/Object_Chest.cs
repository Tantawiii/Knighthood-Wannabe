using UnityEngine;

public class Object_Chest : MonoBehaviour, IDamagable
{
    Rigidbody2D rb => GetComponentInChildren<Rigidbody2D>();
    Animator animator => GetComponentInChildren<Animator>();
    Entity_VFX entityVFX => GetComponent<Entity_VFX>();
    Entity_DropManager dropManager => GetComponent<Entity_DropManager>();

    [Header("Open Details")]
    [SerializeField] Vector2 openKnockback = new Vector2(0,3);
    [SerializeField] private bool canDropItems = true;

    public bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        if(!canDropItems) return false;
        
        dropManager?.DropItems();
        entityVFX.PlayOnDamageVFX();
        animator.SetBool("chestOpen", true);
        rb.linearVelocity = openKnockback;
        rb.angularVelocity = Random.Range(-200f, 200f);
        return true;
    }
}
