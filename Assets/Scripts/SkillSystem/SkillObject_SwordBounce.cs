using System.Collections.Generic;
using UnityEngine;

public class SkillObject_SwordBounce : SkillObject_Sword
{
    [SerializeField] private float bounceSpeed = 15f;
    int bounceCount;

    private Collider2D[] enemyTargets;
    private Transform nextTarget;
    private List<Transform> selectedBefore = new List<Transform>();

    public override void SetupSword(Skill_SwordThrow swordManager, Vector2 direction)
    {
        anim.SetTrigger("spin");
        base.SetupSword(swordManager, direction);

        bounceSpeed = swordManager.bounceSpeed;
        bounceCount = swordManager.bounceCount;
    }

    protected override void Update()
    {
        HandleComeback();
        HandleBounce();
    }

    private void HandleBounce()
    {
        if (nextTarget == null) return;

        transform.position = Vector2.MoveTowards(transform.position, nextTarget.position, bounceSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, nextTarget.position) < 0.75f)
        {
            DamageEnemiesInRadius(transform, 1f); // Damage enemies in a radius of 1 unit around the sword's position
            BounceToNextTarget();

            if(bounceCount == 0 || nextTarget == null)
            {
                nextTarget = null;
                GetSwordBackToPlayer();
            }
        }
    }

    private void BounceToNextTarget()
    {
        nextTarget = GetNextTarget();
        bounceCount--;
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if(enemyTargets == null)
        {
            enemyTargets = EnemiesAround(transform, 10);
            rb.simulated = false; // Stop the sword's physics simulation
        }
        
        DamageEnemiesInRadius(transform, 1f); // Damage enemies in a radius of 1 unit around the sword's position

        if(enemyTargets.Length <= 1 || bounceCount == 0)
        {
            GetSwordBackToPlayer();
        }
        else
        {
            nextTarget = GetNextTarget();
        }
    }

    private Transform GetNextTarget()
    {
        List<Transform> validTargets = GetValidTargets();

        int randomIndex = Random.Range(0, validTargets.Count);

        Transform nextTarget = validTargets[randomIndex];
        selectedBefore.Add(nextTarget);

        return nextTarget;
    }

    private List<Transform> GetValidTargets()
    {
        List<Transform> validTargets = new List<Transform>();
        List<Transform> aliveTargets = GetAliveTargets();

        foreach (var target in enemyTargets)
        {
            if (target != null && !selectedBefore.Contains(target.transform))
            {
                validTargets.Add(target.transform);
            }
        }

        if (validTargets.Count > 0)
        {
            return validTargets;
        }
        else
        {
            selectedBefore.Clear();
            return aliveTargets;
        }
        
    }
    

    private List<Transform> GetAliveTargets()
    {
        List<Transform> aliveTargets = new List<Transform>();

        foreach (var target in enemyTargets)
        {
            if (target != null)
            {
                aliveTargets.Add(target.transform);
            }
        }

        return aliveTargets;
    }
}
