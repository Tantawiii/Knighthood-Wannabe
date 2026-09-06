using UnityEngine;

public class Skill_Base : MonoBehaviour
{
    public Player_SkillManager skillManager { get; private set; }
    public Player player { get; private set; }

    public DamageScaleData damageScaleData { get; private set; }

    [Header("General Details")]
    [SerializeField] private SkillType skillType;
    [SerializeField] protected SkillUpgradeType upgradeType;
    [SerializeField] protected float cooldown;
    [SerializeField] private Skill_DataSO skillData;
    private float lastTimeUsed;

    protected virtual void Awake()
    {
        skillManager = GetComponentInParent<Player_SkillManager>();
        player = GetComponentInParent<Player>();
        lastTimeUsed -= cooldown;
        damageScaleData = new DamageScaleData(); // Initialize with default values
    }

    protected virtual void Start()
    {
        // Deferred to Start so Player.ui, UI.inGameUI and UI_InGame.skillSlots
        // are all initialized before SetSkillUpgrade touches the in-game UI.
        if (skillData != null && skillData.unlockedByDefault)
            SetSkillUpgrade(skillData);
    }

    public virtual void TryUseSkill()
    {

    }

    public void SetSkillUpgrade(Skill_DataSO skillData)
    {
        UpgradeData upgradeData = skillData.upgradeData;

        this.upgradeType = upgradeData.upgradeType;
        this.cooldown = upgradeData.cooldown;
        damageScaleData = upgradeData.damageScaleData;

        player.ui.inGameUI.GetSkillSlot(skillType)?.SetUpSkillSlot(skillData);

        ResetCooldown();
    }

    public virtual bool CanUseSkill()
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
    public void SetSkillOnCooldown()
    {
        player.ui.inGameUI.GetSkillSlot(skillType)?.StartCooldown(cooldown);

        lastTimeUsed = Time.time;
    }
    public void ReduceCooldownBy(float cooldownReduction) => lastTimeUsed += cooldownReduction;
    public void ResetCooldown() 
    {
        player.ui.inGameUI.GetSkillSlot(skillType)?.ResetCooldown();

        lastTimeUsed = Time.time - cooldown;
    }
}
