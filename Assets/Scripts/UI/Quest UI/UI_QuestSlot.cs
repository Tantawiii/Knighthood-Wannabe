using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_QuestSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI questName;
    [SerializeField] private Image[] rewardQuickPreviewSlots;

    public Quest_DataSO questInSlot { get; private set; }
    private UI_QuestPreview questPreview;

    public void SetupQuestSlot(Quest_DataSO questDataSO)
    {
        questPreview = transform.root.GetComponentInChildren<UI_Quest>().GetQuestPreview();

        questInSlot = questDataSO;
        questName.text = questDataSO.questName;

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
            slot.GetComponentInChildren<TextMeshProUGUI>().text = questDataSO.questRewards[i].stackSize == 0 ? string.Empty : questDataSO.questRewards[i].stackSize.ToString();
        }
    }

    public void UpdateQuestPreview()
    {
        questPreview.SetupQuestPreview(questInSlot);
    }
}
