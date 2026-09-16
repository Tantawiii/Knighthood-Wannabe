using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_QuestSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questName;
    [SerializeField] private TextMeshProUGUI questRewardGold;
    [SerializeField] private Image[] rewardQuickPreviewSlots;

    private Quest_DataSO questInSlot;

    public void SetupQuestSlot(Quest_DataSO questDataSO)
    {
        questInSlot = questDataSO;
        questName.text = questDataSO.questName;
        questRewardGold.text = questDataSO.goldReward.ToString();

        foreach(var previewIcon in rewardQuickPreviewSlots)
        {
            previewIcon.gameObject.SetActive(false);
        }

        for(int i = 0; i < questInSlot.questRewards.Length; i++)
        {
            if(questDataSO.questRewards[i] == null || questDataSO.questRewards[i].itemData == null)
            {
                continue;
            }

            Image slot = rewardQuickPreviewSlots[i];
            slot.gameObject.SetActive(true);
            slot.sprite = questDataSO.questRewards[i].itemData.itemIcon;
            slot.GetComponentInChildren<TextMeshProUGUI>().text = questDataSO.questRewards[i].stackSize.ToString();
        }
    }

    public void UpdateQuestPreview()
    {
        Debug.Log("Updating quest preview for: " + questInSlot.questName);
    }
}
