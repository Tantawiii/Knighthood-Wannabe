using UnityEngine;

public class Player_SkillManager : MonoBehaviour
{
    public Skill_Dash dash { get; private set; }
    public Skill_Shard shard { get; private set; }
    public Skill_SwordThrow swordThrow { get; private set; }
    public Skill_TimeEcho timeEcho { get; private set; }
    public Skill_DomainExpansion domainExpansion { get; private set; }

    private Skill_Base[] allSkills;

    private void Awake()
    {
        dash = GetComponentInChildren<Skill_Dash>();
        shard = GetComponentInChildren<Skill_Shard>();
        swordThrow = GetComponentInChildren<Skill_SwordThrow>();
        timeEcho = GetComponentInChildren<Skill_TimeEcho>();
        domainExpansion = GetComponentInChildren<Skill_DomainExpansion>();

        allSkills = GetComponentsInChildren<Skill_Base>();
    }

    public void ReduceAllSkillsCooldownBy(float cooldownReduction)
    {
        foreach (var skill in allSkills)
        {
            skill.ReduceCooldownBy(cooldownReduction);
        }
    }

    public Skill_Base GetSkillByType(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Dash:
                return dash;
            case SkillType.TimeShard:
                return shard;
            case SkillType.SwordThrow:
                return swordThrow;
            case SkillType.TimeEcho:
                return timeEcho;
            case SkillType.DomainExpansion:
                return domainExpansion;
            default:
                Debug.LogWarning($"Skill type {skillType} not found in Player_SkillManager.");
                return null;
        }
    }
}
