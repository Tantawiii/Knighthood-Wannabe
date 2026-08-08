using System.Collections.Generic;
using UnityEngine;

public class Skill_DomainExpansion : Skill_Base
{
    [SerializeField] private GameObject domainPrefab;

    [Header ("Slowing Down Upgrade")]
    [SerializeField] private float slowDownPercent = 0.8f;
    [SerializeField] private float slowDownDuration = 5f;

    [Header ("Spell Casting Upgrade")]
    [SerializeField] private int spellsToCast = 10;
    [SerializeField] private float spellCastingDomainSlowdown = 1f;
    [SerializeField] private float spellCastingDomainDuration = 8f;
    private float spellCastTimer;
    private float spellPerSecond;

    [Header ("Domain Details")]
    public float maxDomainSize = 10;
    public float domainExpandSpeed = 3;

    private List<Enemy> enemiesInDomain = new List<Enemy>();
    private Transform currentTarget;

    public void CreateDomain()
    {
        spellPerSecond = spellsToCast / GetDomainDuration(); 

        GameObject domain = Instantiate(domainPrefab, transform.position, Quaternion.identity);
        domain.GetComponent<SkillObject_DomainExpansion>().SetUpDomain(this);
    }


    public void DoSpellCasting()
    {
        spellCastTimer -= Time.deltaTime;

        if(currentTarget == null)
        {
            currentTarget = FindTargetInDomain();
        }

        if(currentTarget != null && spellCastTimer <= 0)
        {
            CastSpell(currentTarget);
            spellCastTimer = 1f / spellPerSecond;
            currentTarget = null;
        }
    }

    private void CastSpell(Transform target)
    {
        if(upgradeType == SkillUpgradeType.Domain_EchoSpam)
        {
            Vector3 offset = Random.value < .5f ? new Vector2(1, 0) : new Vector2(-1, 0);
            
            skillManager.timeEcho.CreateTimeEcho(target.position + offset);
        }

        if(upgradeType == SkillUpgradeType.Domain_ShardSpam)
        {
            skillManager.shard.CreateRawShard(target, true);
        }
    }

    private Transform FindTargetInDomain()
    {
        if (enemiesInDomain.Count == 0)
            return null;

        int randomIndex = Random.Range(0, enemiesInDomain.Count);
        Transform target = enemiesInDomain[randomIndex].transform;

        if (target == null)
        {
            enemiesInDomain.RemoveAt(randomIndex);
            return null;
        }

        return target;
    }

    public float GetDomainDuration()
    {
        if(upgradeType == SkillUpgradeType.Domain_SlowingDown)
            return slowDownDuration;
        else
            return spellCastingDomainDuration;
    }

    public float GetSlowDownPercent()
    {
        if(upgradeType == SkillUpgradeType.Domain_SlowingDown)
            return slowDownPercent;
        else
            return spellCastingDomainSlowdown;
    }

    public bool InstantDomain()
    {
        return upgradeType != SkillUpgradeType.Domain_EchoSpam 
        && upgradeType != SkillUpgradeType.Domain_ShardSpam;
    }

    public void AddEnemyToDomain(Enemy enemy)
    {
        if(!enemiesInDomain.Contains(enemy))
            enemiesInDomain.Add(enemy);
    }

    public void ClearEnemiesFromDomain()
    {
        foreach(var enemy in enemiesInDomain)
        {
            enemy.StopSlowDown();
        }

        enemiesInDomain = new List<Enemy>();
    }
}
