using UnityEngine;

public class Entity_Combat : MonoBehaviour
{
    public float damage = 10;

    [Header("Target Detection")]
    [SerializeField] Transform targetCheck;
    [SerializeField] float targetCheckRadius = 1;
    [SerializeField] LayerMask whatIsTarget;
    public void PreformAttack()
    {
        foreach (var target in GetDetectedColliders())
        {
            IDamagable damagable = target.GetComponent<IDamagable>();
            damagable.TakeDamage(damage, transform);
        }
    }

    private Collider2D[] GetDetectedColliders()
    {
        return Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(targetCheck.position, targetCheckRadius);
    }
}
