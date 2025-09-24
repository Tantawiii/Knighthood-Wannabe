using UnityEngine;
using UnityEngine.UI;
public class Entity_Health : MonoBehaviour, IDamagable
{
    Slider healthBar;
    Entity_VFX entityVFX;
    Entity entity;
    Entity_Stats stats;

    [SerializeField] protected float currentHp;
    [SerializeField] protected bool isDead;

    [Header("On Damage Knockback")]
    [SerializeField] Vector2 knockbackPower = new Vector2(1.5f, 2.5f);
    [SerializeField] Vector2 heavyKnockbackPower = new Vector2(7f, 7f);
    [SerializeField] float knockbackDuration = .2f;
    [SerializeField] float heavyKnockbackDuration = .5f;

    [Header("On Heavy Damage")]
    [SerializeField] float heavyDamageThreshold = .3f; // % health you should lose to consider damage as heavy.

    protected virtual void Awake()
    {
        stats = GetComponent<Entity_Stats>();
        healthBar = GetComponentInChildren<Slider>();
        entityVFX = GetComponent<Entity_VFX>();
        entity = GetComponent<Entity>();

        currentHp = stats.GetMaxHealth();
        UpdateHealthBar();
    }

    public virtual bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        if (isDead) return false;

        if (AttackEvaded())
        {
            return false;
        }

        Entity_Stats attackerStats = damageDealer.GetComponent<Entity_Stats>();
        float armorReduction = attackerStats != null ? attackerStats.GetArmorReduction() : 0;

        float mitigation = stats.GetArmorMitigation(armorReduction);
        float physicalDamageTaken = damage * (1 - mitigation);

        float resistance = stats.GetElementalResistance(element);
        float elementalDamageTaken = elementalDamage * (1 - resistance);

        TakeKnockback(damageDealer, physicalDamageTaken);

        ReduceHp(physicalDamageTaken + elementalDamageTaken);

        return true;
    }

    private void TakeKnockback(Transform damageDealer, float finalDamage)
    {
        Vector2 knockback = CalculateKnockback(finalDamage, damageDealer);
        float duration = CalculateDuration(finalDamage);

        entity?.RecieveKnockBack(knockback, duration);
    }

    private bool AttackEvaded() => Random.Range(0, 100) < stats.GetEvasion();

    public void ReduceHp(float damage)
    {
        entityVFX?.PlayOnDamageVFX();
        currentHp -= damage;
        UpdateHealthBar();
        if(currentHp <= 0) Die();
    }

    private void Die()
    {
        isDead = true;
        entity.EntityDeath();
    }

    private void UpdateHealthBar() 
    {
        if (healthBar == null)
            return;

        healthBar.value = currentHp / stats.GetMaxHealth();
    } 
    private Vector2 CalculateKnockback(float damage, Transform damageDealer)
    {
        int direction = transform.position.x > damageDealer.position.x ? 1 : -1;

        Vector2 knockback = IsHeavyDamage(damage) ? heavyKnockbackPower : knockbackPower;

        knockback.x *= direction;

        return knockback;
    }

    private float CalculateDuration(float damage) => IsHeavyDamage(damage) ? heavyKnockbackDuration : knockbackDuration;

    private bool IsHeavyDamage(float damage) => damage / stats.GetMaxHealth() > heavyDamageThreshold;
}
