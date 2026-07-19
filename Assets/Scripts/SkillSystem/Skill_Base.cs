using UnityEngine;

public class Skill_Base : MonoBehaviour
{
    [Header("General Details")]
    [SerializeField] private SkillType skillType;
    [SerializeField] protected SkillUpgradeType upgradeType;
    [SerializeField] protected float cooldown;
    private float lastTimeUsed;

    protected virtual void Awake()
    {
        lastTimeUsed -= cooldown;
    }

    public virtual void TryUseSkill()
    {

    }

    public void SetSkillUpgrade(UpgradeData upgradeData)
    {
        this.upgradeType = upgradeData.upgradeType;
        this.cooldown = upgradeData.cooldown;
    }

    public bool CanUseSkill()
    {
        if(upgradeType == SkillUpgradeType.None)
        {
            return false;
        }
        
        if (OnCooldown())
        {
            return false;
        }
        return true;
    }

    protected bool Unlocked(SkillUpgradeType upgradeType) => this.upgradeType == upgradeType;

    private bool OnCooldown() => Time.time < lastTimeUsed + cooldown;
    public void SetSkillOnCooldown() => lastTimeUsed = Time.time;
    public void ResetCooldownBy(float cooldownReduction) => lastTimeUsed += cooldownReduction;
    public void ResetCooldown() => lastTimeUsed = Time.time;
}
