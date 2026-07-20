using UnityEngine;

public class Entity_Combat : MonoBehaviour
{
    Entity_Stats stats;
    Entity_VFX vfx;

    public DamageScaleData basicDamageScale;

    [Header("Target Detection")]
    [SerializeField] Transform targetCheck;
    [SerializeField] float targetCheckRadius = 1;
    [SerializeField] LayerMask whatIsTarget;

    private void Awake()
    {
        stats = GetComponent<Entity_Stats>();
        vfx = GetComponent<Entity_VFX>();
    }
    public void PreformAttack()
    {
        foreach (var target in GetDetectedColliders())
        {
            IDamagable damagable = target.GetComponent<IDamagable>();

            if (damagable == null)
                continue;

            AttackData attackData = stats.GetAttackData(basicDamageScale);
            Entity_StatusHandler statusHandler = target.GetComponent<Entity_StatusHandler>();

            float physicalDamage = attackData.physicalDamage;
            float elementalDamage = attackData.elementalDamage;

            ElementType element = attackData.elementType;

            bool targetGotHit = damagable.TakeDamage(physicalDamage, elementalDamage, element, transform);

            if (element != ElementType.None)
                statusHandler.ApplyStatusEffect(element, attackData.elementalEffectData);

            if(targetGotHit)
            {
                vfx.CreateOnHitVFX(target.transform, attackData.isCritical, element);
            }
        }
    }

    protected Collider2D[] GetDetectedColliders()
    {
        return Physics2D.OverlapCircleAll(targetCheck.position, targetCheckRadius, whatIsTarget);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(targetCheck.position, targetCheckRadius);
    }
}
