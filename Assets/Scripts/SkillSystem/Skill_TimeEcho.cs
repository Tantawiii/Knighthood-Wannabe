using UnityEngine;

public class Skill_TimeEcho : Skill_Base
{
    [SerializeField] private GameObject timeEchoPrefab;
    [SerializeField] private float timeEchoDuration;

    [Header("Attack Upgrades")]
    [SerializeField] private int maxAttacks = 3;
    [SerializeField] private float duplicateChance = 0.3f;

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