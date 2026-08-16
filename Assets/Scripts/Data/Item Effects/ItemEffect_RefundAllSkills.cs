using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Item Effect/Refund All Skills", fileName = "Item Effect - Refund All Skills")]
public class ItemEffect_RefundAllSkills : ItemEffect_DataSO
{
    public override void ExecuteEffect()
    {
        // UI_SkillTree skillTree = FindFirstObjectByType<UI_SkillTree>(FindObjectsInactive.Include); // This line is can find skill tree if it is inactive
        // skillTree.ResetSkillTree(); 
        UI ui = FindFirstObjectByType<UI>();
        ui.skillTreeUI.ResetSkillTree();
    }
}