using UnityEngine;

public class UI : MonoBehaviour
{
    public UI_SkillToolTip skillToolTip { get; private set; }
    public UI_ItemToolTip itemToolTip { get; private set; }
    public UI_StatToolTip statToolTip { get; private set; }
    public UI_SkillTree skillTreeUI { get; private set; }
    public UI_Inventory inventoryUI { get; private set; }
    public UI_Storage storageUI { get; private set; }
    private bool skillTreeEnabled;
    private bool inventoryEnabled;

    private void Awake()
    {
        skillToolTip = GetComponentInChildren<UI_SkillToolTip>();
        itemToolTip = GetComponentInChildren<UI_ItemToolTip>();
        statToolTip = GetComponentInChildren<UI_StatToolTip>();

        skillTreeUI = GetComponentInChildren<UI_SkillTree>(true); // This line is can find skill tree if it is inactive
        inventoryUI = GetComponentInChildren<UI_Inventory>(true); // This line is can find inventory if it is inactive
        storageUI = GetComponentInChildren<UI_Storage>(true); // This line is can find storage if it is inactive

        skillTreeEnabled = skillTreeUI.gameObject.activeSelf;
        inventoryEnabled = inventoryUI.gameObject.activeSelf;
    }

    public void SwitchOffAllToolTips()
    {
        skillToolTip.ShowToolTip(false, null);
        itemToolTip.ShowToolTip(false, null);
        statToolTip.ShowToolTip(false, null);
    }

    public void ToggleSkillTreeUI()
    {
        skillTreeEnabled = !skillTreeEnabled;
        skillTreeUI.gameObject.SetActive(skillTreeEnabled);
        skillToolTip.ShowToolTip(false, null);
    }

    public void ToggleInventoryUI()
    {
        inventoryEnabled = !inventoryEnabled;
        inventoryUI.gameObject.SetActive(inventoryEnabled);
        itemToolTip.ShowToolTip(false, null);
        statToolTip.ShowToolTip(false, null);
    }
}
