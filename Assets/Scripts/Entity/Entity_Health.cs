using UnityEngine;
using UnityEngine.UI;
public class Entity_Health : MonoBehaviour, IDamagable
{
    Slider healthBar;
    Entity_VFX entityVFX;
    Entity entity;

    [SerializeField] protected float currentHp;
    [SerializeField] protected float maxHp = 100;
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
        healthBar = GetComponentInChildren<Slider>();
        entityVFX = GetComponent<Entity_VFX>();
        entity = GetComponent<Entity>();

        currentHp = maxHp;
        UpdateHealthBar();
    }

    public virtual void TakeDamage(float damage, Transform damageDealer)
    {
        if(isDead) return;

        Vector2 knockback = CalculateKnockback(damage,damageDealer);
        float duration = CalculateDuration(damage);

        entity?.RecieveKnockBack(knockback, duration);
        entityVFX?.PlayOnDamageVFX();

        ReduceHp(damage);
    }

    protected void ReduceHp(float damage)
    {
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

        healthBar.value = currentHp / maxHp;
    } 
    private Vector2 CalculateKnockback(float damage, Transform damageDealer)
    {
        int direction = transform.position.x > damageDealer.position.x ? 1 : -1;

        Vector2 knockback = IsHeavyDamage(damage) ? heavyKnockbackPower : knockbackPower;

        knockback.x *= direction;

        return knockback;
    }

    private float CalculateDuration(float damage) => IsHeavyDamage(damage) ? heavyKnockbackDuration : knockbackDuration;

    private bool IsHeavyDamage(float damage) => damage / maxHp > heavyDamageThreshold;
}
