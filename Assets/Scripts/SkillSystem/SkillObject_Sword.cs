using UnityEngine;

public class SkillObject_Sword : SkillObject_Base
{
    protected Skill_SwordThrow swordManager;

    protected Transform playerTransform;
    protected bool shouldComeback;
    protected float comebackSpeed = 20f;
    protected float maxAllowedDistance = 25f;

    protected virtual void Update()
    {
        transform.right = rb.linearVelocity; // Rotate the sword to face the direction of its velocity
        HandleComeback();
    }

    public virtual void SetupSword(Skill_SwordThrow swordManager, Vector2 direction)
    {

        rb.linearVelocity = direction;

        this.swordManager = swordManager;
        
        playerTransform = swordManager.transform.root; // Assuming the player is the root of the swordManager's transform hierarchy
        playerStats = swordManager.player.entityStats;
        damageScaleData = swordManager.damageScaleData;
    }

    public void GetSwordBackToPlayer() => shouldComeback = true;

    protected void HandleComeback()
    {
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if(distance > maxAllowedDistance)
        {
            GetSwordBackToPlayer();
        }

        if (!shouldComeback) return;

        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, comebackSpeed * Time.deltaTime);

        if (distance < 0.5f)
            Destroy(gameObject);
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
