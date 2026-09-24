using TMPro;
using UnityEngine;

public class UI_ActiveQuestPreview : MonoBehaviour
{
    private Player_QuestManager questManager;

    [SerializeField] private TextMeshProUGUI questName;
    [SerializeField] private TextMeshProUGUI questDescription;
    [SerializeField] private TextMeshProUGUI questRewardGold;
    [SerializeField] private TextMeshProUGUI progress;
    [SerializeField] private UI_QuestRewardSlot[] questRewardSlots;


    public void SetupQuestPreview(QuestData questData)
    {
        questManager = Player.Instance.questManager;
        Quest_DataSO questDataSO = questData.questDataSO;

        questName.text = questDataSO.questName;
        questDescription.text = questDataSO.questDescription;
        questRewardGold.text = questDataSO.goldReward.ToString() + "G.";

        progress.text = questDataSO.questObjective + " " + questManager.GetQuestProgress(questData) + " / " + questDataSO.requiredAmount;

        foreach(var rewardSlot in questRewardSlots)
        {
            rewardSlot.gameObject.SetActive(false);
        }

        for(int i = 0; i < questDataSO.questRewards.Length; i++)
        {
            if(questDataSO.questRewards[i] == null)
            {
                continue;
            }

            questRewardSlots[i].gameObject.SetActive(true);
            questRewardSlots[i].UpdateSlot(questDataSO.questRewards[i]);
        }
    }
}
