using UnityEngine;

public class Skill_DomainExpansion : Skill_Base
{
    public bool InstantDomain()
    {
        return upgradeType != SkillUpgradeType.Domain_EchoSpam 
        && upgradeType != SkillUpgradeType.Domain_ShardSpam;
    }
    public void CreateDomain()
    {
        Debug.Log("Domain Expansion Created");
    }
}
