using UnityEngine;

public class Skill_Base : MonoBehaviour
{
    public Player player { get; private set; }

    public DamageScaleData damageScaleData { get; private set; }

    [Header("General Details")]
    [SerializeField] private SkillType skillType;
    [SerializeField] protected SkillUpgradeType upgradeType;
    [SerializeField] protected float cooldown;
    private float lastTimeUsed;

    protected virtual void Awake()
    {
        player = GetComponentInParent<Player>();
        lastTimeUsed -= cooldown;
    }

    public virtual void TryUseSkill()
    {

    }

    public void SetSkillUpgrade(UpgradeData upgradeData)
    {
        this.upgradeType = upgradeData.upgradeType;
        this.cooldown = upgradeData.cooldown;
        damageScaleData = upgradeData.damageScaleData;
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

    protected bool OnCooldown() => Time.time < lastTimeUsed + cooldown;
    public void SetSkillOnCooldown() => lastTimeUsed = Time.time;
    public void ResetCooldownBy(float cooldownReduction) => lastTimeUsed += cooldownReduction;
    public void ResetCooldown() => lastTimeUsed = Time.time;
}
