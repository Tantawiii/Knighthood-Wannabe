using UnityEngine;

public class Enemy_Health : Entity_Health
{
    Enemy enemy;
    Player_QuestManager questManager;

    protected override void Start()
    {
        base.Start();

        enemy = GetComponent<Enemy>();
        questManager = Player.Instance.questManager;
    }
    public override bool TakeDamage(float damage, float elementalDamage, ElementType element, Transform damageDealer)
    {
        if(!canTakeDamage)
            return false;
        
        bool wasHit = base.TakeDamage(damage, elementalDamage, element, damageDealer);

        if (!wasHit)
            return false;

        if (damageDealer.GetComponent<Player>() != null)
        {
            enemy.EnterBattleState(damageDealer);
        }

        return true;
    }

    protected override void Die()
    {
        base.Die();

        questManager.AddProgress(enemy.questTargetID);
    }
}
