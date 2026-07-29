using UnityEngine;

public class SkillObject_Sword : SkillObject_Base
{
    protected Skill_SwordThrow swordManager;
    protected Rigidbody2D rb;

    private void Update()
    {
        transform.right = rb.linearVelocity; // Rotate the sword to face the direction of its velocity
    }

    public virtual void SetupSword(Skill_SwordThrow swordManager, Vector2 direction)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.linearVelocity = direction;

        this.swordManager = swordManager;
        
        playerStats = swordManager.player.entityStats;
        damageScaleData = swordManager.damageScaleData;
    }
    
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        StopSword(collision);
        DamageEnemiesInRadius(transform, 1f); // Damage enemies in a radius of 1 unit around the sword's position
    }

    protected void StopSword(Collider2D collision)
    {
        rb.simulated = false; // Stop the sword's physics simulation
        transform.SetParent(collision.transform); // Attach the sword to the object it collided with
    }
}
