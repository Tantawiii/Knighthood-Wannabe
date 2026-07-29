using UnityEngine;

public class SkillObject_SwordPierce : SkillObject_Sword
{
    private int amountToPierce;

    public override void SetupSword(Skill_SwordThrow swordManager, Vector2 direction)
    {
        base.SetupSword(swordManager, direction);
        amountToPierce = swordManager.pierceAmountOfEnemies; // Set the amount of enemies the sword can pierce through
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        bool groundHit = collision.gameObject.layer == LayerMask.NameToLayer("Ground");
        if(amountToPierce <= 0 || groundHit)
        {
            DamageEnemiesInRadius(transform, .3f); // Damage enemies in the radius of the sword's impact
            StopSword(collision); // Stop the sword if it has pierced through the maximum number of enemies and hits the ground
            return;
        }

        amountToPierce--; // Decrease the amount of enemies the sword can pierce through
        DamageEnemiesInRadius(transform, .3f); // Damage enemies in the radius of the sword
    }
}
