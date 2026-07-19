using System.Collections;
using UnityEngine;

public class Skill_Shard : Skill_Base
{
    private SkillObject_Shard currentShard;

    [SerializeField] private GameObject shardPrefab;
    [SerializeField] private float shardDetonationTime = 2f;

    [Header("Moving Shard Upgrade")]
    [SerializeField] private float movingShardSpeed = 7f;

    [Header("Multi Cast Upgrade")]
    [SerializeField] private int maxCharges = 3;
    [SerializeField] private int currentCharges;
    [SerializeField] private bool isRecharging;

    protected override void Awake()
    {
        base.Awake();
        currentCharges = maxCharges;
    }
    
    public override void TryUseSkill()
    {
        if(!CanUseSkill())
            return;

        if(Unlocked(SkillUpgradeType.Shard))
        {
            HandleShardRegular();
        }

        if(Unlocked(SkillUpgradeType.Shard_MoveToEnemy))
        {
            HandleShardMoving();
        }

        if(Unlocked(SkillUpgradeType.Shard_MultiCast))
        {
            HandleShardMultiCast();
        }
    }

    private void HandleShardMultiCast()
    {
        if(currentCharges <= 0)
            return;
        
        CreateShard();
        currentShard.MoveTowardsClosestTarget(movingShardSpeed);
        currentCharges--;

        if(!isRecharging)
            StartCoroutine(RechargeShardCo());
    }

    private IEnumerator RechargeShardCo()
    {
        isRecharging = true;
        while(currentCharges < maxCharges)
        {
            yield return new WaitForSeconds(cooldown);
            currentCharges++;
        }
        isRecharging = false;
    }

    private void HandleShardMoving()
    {
        CreateShard();
        currentShard.MoveTowardsClosestTarget(movingShardSpeed);
        SetSkillOnCooldown();
    }

    private void HandleShardRegular()
    {
        CreateShard();
        SetSkillOnCooldown();
    }

    public void CreateShard()
    {   
        if (shardPrefab != null)
        {
            GameObject shard = Instantiate(shardPrefab, transform.position, Quaternion.identity);
            currentShard = shard.GetComponent<SkillObject_Shard>();
            currentShard.SetUpShard(shardDetonationTime);
        }
    }
}
