using UnityEngine;
using UnityEngine.UI;
public class Entity_Health : MonoBehaviour, IDamagable
{
    Slider healthBar;
    Entity_VFX entityVFX;
    Entity entity;
    Entity_Stats entityStats;

    [SerializeField] protected float currentHealth;
    [SerializeField] protected bool isDead;
    [Header("Health Regenerate")]
    [SerializeField] float regenInterval = 1;
    [SerializeField] bool canRegenerateHealth = true;

    [Header("On Damage Knockback")]
    [SerializeField] Vector2 knockbackPower = new Vector2(1.5f, 2.5f);
    [SerializeField] Vector2 heavyKnockbackPower = new Vector2(7f, 7f);
    [SerializeField] float knockbackDuration = .2f;
    [SerializeField] float heavyKnockbackDuration = .5f;

    [Header("On Heavy Damage")]
    [SerializeField] float heavyDamageThreshold = .3f; // % health you should lose to consider damage as heavy.

    protected virtual void Awake()
    {
        entityStats = GetComponent<Entity_Stats>();
        healthBar = GetComponentInChildren<Slider>();
        entityVFX = GetComponent<Entity_VFX>();
        entity = GetComponent<Entity>();

        currentHealth = entityStats.GetMaxHealth();
        UpdateHealthBar();
        InvokeRepeating(nameof(RegenerateHealth), 0, regenInterval);
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

        float mitigation = entityStats.GetArmorMitigation(armorReduction);
        float physicalDamageTaken = damage * (1 - mitigation);

        float resistance = entityStats.GetElementalResistance(element);
        float elementalDamageTaken = elementalDamage * (1 - resistance);

        TakeKnockback(damageDealer, physicalDamageTaken);

        ReduceHealth(physicalDamageTaken + elementalDamageTaken);

        return true;
    }

    private bool AttackEvaded() => Random.Range(0, 100) < entityStats.GetEvasion();

    private void RegenerateHealth()
    {
        if (!canRegenerateHealth)
            return;

        float regenAmount = entityStats.resourceGroup.healthRegen.GetValue();
        IncreaseHealth(regenAmount);
        UpdateHealthBar();
    }

    public void IncreaseHealth(float healAmount)
    {
        if (isDead)
            return;

        float newHealth = currentHealth + healAmount;
        float maxHealth = entityStats.GetMaxHealth();
        
        currentHealth = Mathf.Min(newHealth, maxHealth);
    }

    public void ReduceHealth(float damage)
    {
        entityVFX?.PlayOnDamageVFX();
        currentHealth -= damage;
        UpdateHealthBar();
        if(currentHealth <= 0) Die();
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

        healthBar.value = currentHealth / entityStats.GetMaxHealth();
    }

    private void TakeKnockback(Transform damageDealer, float finalDamage)
    {
        Vector2 knockback = CalculateKnockback(finalDamage, damageDealer);
        float duration = CalculateDuration(finalDamage);

        entity?.RecieveKnockBack(knockback, duration);
    }
    private Vector2 CalculateKnockback(float damage, Transform damageDealer)
    {
        int direction = transform.position.x > damageDealer.position.x ? 1 : -1;

        Vector2 knockback = IsHeavyDamage(damage) ? heavyKnockbackPower : knockbackPower;

        knockback.x *= direction;

        return knockback;
    }

    private float CalculateDuration(float damage) => IsHeavyDamage(damage) ? heavyKnockbackDuration : knockbackDuration;

    private bool IsHeavyDamage(float damage) => damage / entityStats.GetMaxHealth() > heavyDamageThreshold;
}
