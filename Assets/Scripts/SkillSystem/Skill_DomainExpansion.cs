using UnityEngine;

public class Skill_DomainExpansion : Skill_Base
{
    [SerializeField] private GameObject domainPrefab;
    public bool InstantDomain()
    {
        return upgradeType != SkillUpgradeType.Domain_EchoSpam 
        && upgradeType != SkillUpgradeType.Domain_ShardSpam;
    }
    public void CreateDomain()
    {
        GameObject domain = Instantiate(domainPrefab, transform.position, Quaternion.identity);
        domain.GetComponent<SkillObject_DomainExpansion>().SetUpDomain(this);
    }
}
