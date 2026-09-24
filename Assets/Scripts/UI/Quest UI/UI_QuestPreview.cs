using TMPro;
using UnityEngine;

public class UI_QuestPreview : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questName;
    [SerializeField] private TextMeshProUGUI questDescription;
    [SerializeField] private TextMeshProUGUI questGoal;
    [SerializeField] private TextMeshProUGUI questRewardGold;
    [SerializeField] private UI_QuestRewardSlot[] questReward;

    [SerializeField] private GameObject[] additionalObjects;
    private UI_Quest questUI;
    private Quest_DataSO previewQuest;

    public void SetupQuestPreview(Quest_DataSO questDataSO)
    {
        questUI = transform.root.GetComponentInChildren<UI_Quest>();
        previewQuest = questDataSO;

        EnableAdditionalObjects(true);

        questName.text = questDataSO.questName;
        questDescription.text = questDataSO.questDescription;
        questGoal.text = questDataSO.questObjective + " " + questDataSO.requiredAmount;
        questRewardGold.text = questDataSO.goldReward.ToString() + "G.";

        EnableQuestRewardObj(false);

        for(int i = 0; i < questDataSO.questRewards.Length; i++)
        {
            Inventory_Item rewardItem = new Inventory_Item(questDataSO.questRewards[i].itemData);
            rewardItem.stackSize = questDataSO.questRewards[i].stackSize;

            questReward[i].gameObject.SetActive(true);
            questReward[i].UpdateSlot(rewardItem);
        }
    }

    public void AcceptQuestBTN()
    {
        MakeQuestPreviewEmpty();

        questUI.questManager.AcceptQuest(previewQuest);
        questUI.UpdateQuestList();
    }

    public void MakeQuestPreviewEmpty()
    {
        questName.text = string.Empty;
        questDescription.text = string.Empty;
        questGoal.text = string.Empty;
        questRewardGold.text = string.Empty;

        EnableAdditionalObjects(false);
        EnableQuestRewardObj(false);
    }

    private void EnableAdditionalObjects(bool enable)
    {
        foreach (var obj in additionalObjects)
        {
            obj.SetActive(enable);
        }
    }

    private void EnableQuestRewardObj(bool enable)
    {
        foreach (var rewardSlot in questReward)
        {
            rewardSlot.gameObject.SetActive(enable);
        }
    }
}
