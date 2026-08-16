using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item Effect/Grant Skill Point", fileName = "Item effect data - grant skill point")]
public class ItemEffect_GrantSkillPoint : ItemEffect_DataSO
{
    [SerializeField] private int skillPointsToGrant;

    public override void ExecuteEffect()
    {
        UI ui = FindFirstObjectByType<UI>();

        ui.skillTreeUI.AddSkillPoints(skillPointsToGrant);
    }
}
