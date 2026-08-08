using UnityEngine;

public class Skill_TimeEcho : Skill_Base
{
    [SerializeField] private GameObject timeEchoPrefab;
    [SerializeField] private float timeEchoDuration;

    [Header("Attack Upgrades")]
    [SerializeField] private int maxAttacks = 3;
    [SerializeField] private float duplicateChance = 0.3f;

    [Header("Heal Wisp Upgrades")]
    [SerializeField] private float damagePercentHealed = .3f;
    [SerializeField] private float cooldownReduction;

    public float GetPercentOfDamageHealed()
    {
        if(ShouldBeWisp() == false)
        {
            return 0;
        }
        return damagePercentHealed;
    }

    public float GetCooldownReduction()
    {
        if(upgradeType != SkillUpgradeType.TimeEcho_CooldownWisp)
        {
            return 0;
        }
        return cooldownReduction;
    }

    public bool CanRemoveNegativeEffects()
    {
        return upgradeType == SkillUpgradeType.TimeEcho_CleanseWisp;
    }

    public bool ShouldBeWisp()
    {
        return upgradeType == SkillUpgradeType.TimeEcho_HealWisp 
        || upgradeType == SkillUpgradeType.TimeEcho_CleanseWisp 
        || upgradeType == SkillUpgradeType.TimeEcho_CooldownWisp;
    }

    public float GetDuplicateChance()
    {
        if(upgradeType != SkillUpgradeType.TimeEcho_DuplicateChance)
        {
            return 0;
        }
        return duplicateChance;
    }

    public int GetMaxAttacks()
    {
        if(upgradeType == SkillUpgradeType.TimeEcho_SingleAttack || upgradeType == SkillUpgradeType.TimeEcho_DuplicateChance)
        {
            return 1;
        }
        if(upgradeType == SkillUpgradeType.TimeEcho_MultiAttack)
        {
            return maxAttacks;
        }
        return 0;
    }

    public float GetTimeEchoDuration() => timeEchoDuration;

    public override void TryUseSkill()
    {
        if(!CanUseSkill())
        {
            return;
        }

        CreateTimeEcho();
    }

    public void CreateTimeEcho(Vector3 targetPosition = default(Vector3))
    {
        Vector3 positionToSpawn = targetPosition != default(Vector3) ? targetPosition : transform.position;

        GameObject timeEchoInstance = Instantiate(timeEchoPrefab, positionToSpawn, Quaternion.identity);
        timeEchoInstance.GetComponent<SkillObject_TimeEcho>().SetUpEcho(this);
    }
}