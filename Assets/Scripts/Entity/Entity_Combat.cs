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

    [Header("Status Effect Details")]
    [SerializeField] float defaultDuration = 3f;
    [SerializeField] float chillSlowMultiplier = .4f;
    [SerializeField] float electrifyChargeBuildUp = .4f;
    [Space]
    [SerializeField] float fireScale = .8f;
    [SerializeField] float lightningScale = 2.5f;

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

            ElementalEffectData effectData = new ElementalEffectData(stats, basicDamageScale);

            float elementalDamage = stats.GetElementalDamage(out ElementType element, .6f);
            float damage = stats.GetPhysicalDamage(out bool isCrit);
            bool targetGotHit = damagable.TakeDamage(damage, elementalDamage, element, transform);

            if (element != ElementType.None)
                target.GetComponent<Entity_StatusHandler>().ApplyStatusEffect(element, effectData);

            if(targetGotHit)
            {
                vfx.UpdateOnHitColor(element);
                vfx.CreateOnHitVFX(target.transform, isCrit);
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
