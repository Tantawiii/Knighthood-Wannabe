using System.Collections.Generic;
using UnityEngine;

public class Skill_DomainExpansion : Skill_Base
{
    [SerializeField] private GameObject domainPrefab;

    [Header ("Slowing Down Upgrade")]
    [SerializeField] private float slowDownPercent = 0.8f;
    [SerializeField] private float slowDownDuration = 5f;

    [Header ("Shard Cast Upgrade")]
    [SerializeField] private int shardsToCast = 10;
    [SerializeField] private float shardCastDomainSlowDown = 1f;
    [SerializeField] private float shardCastDomainDuration = 8f;
    private float spellCastTimer;
    private float spellPerSecond;

    [Header ("Time Echo Cast Upgrade")]
    [SerializeField] private int timeEchosToCast = 8;
    [SerializeField] private float timeEchoDomainSlowDown = 1f;
    [SerializeField] private float timeEchoDomainDuration = 6f;
    [SerializeField] private float healthToRestoreWithEcho = .05f;

    [Header ("Domain Details")]
    public float maxDomainSize = 10;
    public float domainExpandSpeed = 3;

    private List<Enemy> enemiesInDomain = new List<Enemy>();
    private Transform currentTarget;

    public void CreateDomain()
    {
        spellPerSecond = GetSpellsToCast() / GetDomainDuration(); 

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
        enemiesInDomain.RemoveAll(enemy => enemy == null || enemy.health.isDead);
        if(enemiesInDomain.Count == 0)
            return null;

        int randomIndex = Random.Range(0, enemiesInDomain.Count);
        return enemiesInDomain[randomIndex].transform;
    }

    public float GetDomainDuration()
    {
        if(upgradeType == SkillUpgradeType.Domain_SlowingDown)
            return slowDownDuration;
        else if(upgradeType == SkillUpgradeType.Domain_EchoSpam)
            return timeEchoDomainDuration;
        else if(upgradeType == SkillUpgradeType.Domain_ShardSpam)
            return shardCastDomainDuration;
        else
            return 0;
    }

    public float GetSlowDownPercent()
    {
        if(upgradeType == SkillUpgradeType.Domain_SlowingDown)
            return slowDownPercent;
        else if(upgradeType == SkillUpgradeType.Domain_EchoSpam)
            return timeEchoDomainSlowDown;
        else if(upgradeType == SkillUpgradeType.Domain_ShardSpam)
            return shardCastDomainSlowDown;
        else
            return 0;
    }

    private int GetSpellsToCast()
    {
        if(upgradeType == SkillUpgradeType.Domain_EchoSpam)
            return timeEchosToCast;
        else if(upgradeType == SkillUpgradeType.Domain_ShardSpam)
            return shardsToCast;
        else
            return 0;
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
