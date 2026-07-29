using UnityEngine;

public class Player_SkillManager : MonoBehaviour
{
    public Skill_Dash dash { get; private set; }
    public Skill_Shard shard { get; private set; }
    public Skill_SwordThrow swordThrow { get; private set; }

    private void Awake()
    {
        dash = GetComponentInChildren<Skill_Dash>();
        shard = GetComponentInChildren<Skill_Shard>();
        swordThrow = GetComponentInChildren<Skill_SwordThrow>();
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
            default:
                Debug.LogWarning($"Skill type {skillType} not found in Player_SkillManager.");
                return null;
        }
    }
}
