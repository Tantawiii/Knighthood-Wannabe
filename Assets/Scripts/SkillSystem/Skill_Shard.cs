using System.Collections;
using UnityEngine;

public class Skill_Shard : Skill_Base
{
    private SkillObject_Shard currentShard;
    private Entity_Health playerHealth;

    [SerializeField] private GameObject shardPrefab;
    [SerializeField] private float shardDetonationTime = 2f;

    [Header("Moving Shard Upgrade")]
    [SerializeField] private float movingShardSpeed = 7f;

    [Header("Multi Cast Upgrade")]
    [SerializeField] private int maxCharges = 3;
    [SerializeField] private int currentCharges;
    [SerializeField] private bool isRecharging;

    [Header("Teleport Shard")]
    [SerializeField] private float shardExistDuration = 10f;

    [Header("Health Rewind Upgrade")]
    [SerializeField] private float healthRewindAmount;

    protected override void Awake()
    {
        base.Awake();
        currentCharges = maxCharges;
        playerHealth = GetComponentInParent<Entity_Health>();
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

        if(Unlocked(SkillUpgradeType.Shard_Teleport))
        {
            HandleShardTeleport();
        }

        if(Unlocked(SkillUpgradeType.Shard_TeleportHpRewind))
        {
            HandleShardTeleportHpRewind();
        }
    }

    void HandleShardTeleportHpRewind()
    {
        if(currentShard == null)
        {
            CreateShard();
            healthRewindAmount = playerHealth.GetCurrentHealth();
        }
        else
        {
            SwapPlayerPosition();
            playerHealth.SetCurrentHealth(healthRewindAmount);
            SetSkillOnCooldown();
        }
    }

    void HandleShardTeleport()
    {
        if(currentShard == null)
        {
            CreateShard();
        }
        else
        {
            SwapPlayerPosition();
            SetSkillOnCooldown();
        }
    }

    void SwapPlayerPosition()
    {
        Vector3 shardPosition = currentShard.transform.position;
        Vector3 playerPosition = player.transform.position;

        currentShard.transform.position = playerPosition;
        currentShard.Explode();
        player.TeleportPlayer(shardPosition);

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
            float detonationTime = GetDetonationTime();
            GameObject shard = Instantiate(shardPrefab, transform.position, Quaternion.identity);
            currentShard = shard.GetComponent<SkillObject_Shard>();
            currentShard.SetUpShard(this);

            if(Unlocked(SkillUpgradeType.Shard_Teleport) || Unlocked(SkillUpgradeType.Shard_TeleportHpRewind))
            {
                currentShard.OnShardExploded += ForceCooldown;
            }
        }
    }

    public float GetDetonationTime()    
    {
        if(Unlocked(SkillUpgradeType.Shard_Teleport) || Unlocked(SkillUpgradeType.Shard_TeleportHpRewind))
        {
            return shardExistDuration;
        }

        return shardDetonationTime;
    }

    private void ForceCooldown()
    {
        if(!OnCooldown())
        {
            SetSkillOnCooldown();
            currentShard.OnShardExploded -= ForceCooldown;
        }
    }
}
