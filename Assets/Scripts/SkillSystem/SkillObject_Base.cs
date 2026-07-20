using UnityEngine;

public class SkillObject_Base : MonoBehaviour
{
    [SerializeField] protected LayerMask whatIsEnemy;
    [SerializeField] protected Transform targetCheck;
    [SerializeField] protected float targetCheckRadius = 1f;

    protected Entity_Stats playerStats;
    protected DamageScaleData damageScaleData;

    protected Collider2D[] EnemiesAround(Transform t, float radius)
    {
        return Physics2D.OverlapCircleAll(t.position, radius, whatIsEnemy);
    }

    protected virtual void OnDrawGizmos()
    {
        if (targetCheck == null)
            targetCheck = transform;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(targetCheck.position, targetCheckRadius);
    }

    protected void DamageEnemiesInRadius(Transform t, float radius)
    {
        foreach (var target in EnemiesAround(t, radius))
        {
            IDamagable damageable = target.GetComponent<IDamagable>();
            if (damageable == null)
            {
                continue;
            }

            ElementalEffectData elementalEffectData = new ElementalEffectData(playerStats, damageScaleData);
            float physicalDamage = playerStats.GetPhysicalDamage(out bool isCrit, damageScaleData.physical);
            float elementalDamage = playerStats.GetElementalDamage(out ElementType elementType, damageScaleData.elemental);

            damageable.TakeDamage(physicalDamage, elementalDamage, elementType, transform);

            if(elementType != ElementType.None)
            {
                target.GetComponent<Entity_StatusHandler>().ApplyStatusEffect(elementType, elementalEffectData);
            }
        }
    }

    protected Transform FindClosestTarget()
    {
        Transform closestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (var enemy in EnemiesAround(transform, 10))
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestTarget = enemy.transform;
                closestDistance = distance;
            }
        }

        return closestTarget;
    }
}
