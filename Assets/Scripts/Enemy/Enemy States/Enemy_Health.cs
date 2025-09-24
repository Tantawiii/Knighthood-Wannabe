using UnityEngine;

public class Enemy_Health : Entity_Health
{
    Enemy enemy => GetComponent<Enemy>();
    public override bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        bool wasHit = base.TakeDamage(damage, elementalDamage, element, damageDealer);

        if (!wasHit)
            return false;

        if (damageDealer.GetComponent<Player>() != null)
        {
            enemy.EnterBattleState(damageDealer);
        }

        return true;
    }
}
