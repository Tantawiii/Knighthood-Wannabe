using UnityEngine;

public class SkillObject_SwordSpin : SkillObject_Sword
{
    private int maxDistance;
    private float attackPerSecond;
    private float attackTimer;

    public override void SetupSword(Skill_SwordThrow swordManager, Vector2 direction)
    {
        base.SetupSword(swordManager, direction);

        anim.SetTrigger("spin"); // Trigger the spin animation for the sword

        maxDistance = swordManager.maxDistance;
        attackPerSecond = swordManager.attackPerSecond;

        Invoke(nameof(GetSwordBackToPlayer), swordManager.maxSpinDuration); // Schedule the sword to return to the player after the max spin duration
    }

    protected override void Update()
    {
        HandleAttack();
        HandleStopping();
        HandleComeback();
    }

    private void HandleStopping()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        if(distanceToPlayer > maxDistance && rb.simulated == true)
        {
            rb.simulated = false; // Stop the sword's movement if it exceeds the maximum distance from the player
        }
    }

    private void HandleAttack()
    {
        attackTimer -= Time.deltaTime;
        if(attackTimer < 0f)
        {
            DamageEnemiesInRadius(transform, 1f); // Damage enemies in the radius of the sword's spin
            attackTimer = 1f / attackPerSecond; // Reset the attack timer based on the attack rate
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        rb.simulated = false; // Stop the sword's movement when it collides with an object
    }
}
