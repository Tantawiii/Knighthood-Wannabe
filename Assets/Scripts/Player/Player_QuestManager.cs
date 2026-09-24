using System.Collections.Generic;
using UnityEngine;

public class Player_QuestManager : MonoBehaviour, ISaveable
{
    public List<QuestData> activeQuests;
    public List<QuestData> completedQuests;

    [Header("Quest Database")]
    [SerializeField] private QuestDatabase_DataSO questDatabase;
    private Entity_DropManager dropManager;
    private Inventory_Player inventory;

    private void Awake()
    {
        dropManager = GetComponent<Entity_DropManager>();
        inventory = GetComponent<Inventory_Player>();
    }

    public void TryGiveRewardFrom(RewardGiver npcType)
    {
        List<QuestData> getRewardQuests = new List<QuestData>();

        foreach(var quest in activeQuests)
        {
            // Deliver Items if Can.
            if(quest.questDataSO.questType == QuestType.Deliver)
            {
                var deliverItem = quest.questDataSO.itemToDeliver;
                var requiredAmount = quest.questDataSO.requiredAmount;

                if(inventory.HasItemAmount(deliverItem, requiredAmount))
                {
                    inventory.RemoveItemAmount(deliverItem, requiredAmount);
                    quest.AddQuestProgress(requiredAmount);
                }
            }

            if(quest.CanGetReward() && quest.questDataSO.rewardGiver == npcType)
            {
                getRewardQuests.Add(quest);
            }
        }

        foreach(var quest in getRewardQuests)
        {
            GiveQuestReward(quest.questDataSO);
            CompleteQuest(quest);
        }
    }

    private void GiveQuestReward(Quest_DataSO questDataSO)
    {
        foreach(var item in questDataSO.questRewards)
        {
            if(item == null || item.itemData == null)
            {
                continue;
            }

            for(int i = 0; i < item.stackSize; i++)
            {
                dropManager.CreateItemDrop(item.itemData);
            }
        }
    }

    public void AddProgress(string questTargetId, int amount = 1)
    {
        List<QuestData> getRewardQuests = new List<QuestData>();

        foreach(var quest in activeQuests)
        {
            if(quest.questDataSO.questTargetID != questTargetId)
            {
                continue;
            }

            if(!quest.CanGetReward())
            {
                quest.AddQuestProgress(amount);
            }

            if(quest.questDataSO.rewardGiver == RewardGiver.None && quest.CanGetReward())
            {
                getRewardQuests.Add(quest);
            }
        }

        foreach(var quest in getRewardQuests)
        {
            GiveQuestReward(quest.questDataSO);
            CompleteQuest(quest);
        }
    }

    public int GetQuestProgress(QuestData questToCheck)
    {
        QuestData quest = activeQuests.Find(q => q == questToCheck);

        return quest != null ? quest.currentAmount : 0;
    }

    public void AcceptQuest(Quest_DataSO questDataSO)
    {
        activeQuests.Add(new QuestData(questDataSO));
    }

    public void CompleteQuest(QuestData quest)
    {
        completedQuests.Add(quest);
        activeQuests.Remove(quest);
    }

    public bool QuestIsActive(Quest_DataSO questToCheck)
    {
        if(questToCheck == null)
        {
            return false;
        }

        return activeQuests.Find(q => q.questDataSO == questToCheck) != null;
    }

    public void LoadData(GameData data)
    {
        activeQuests.Clear();

        foreach(var entry in data.activeQuests)
        {
            string questSaveID = entry.Key;
            int progress = entry.Value;

            Quest_DataSO questDataSO = questDatabase.GetQuestDataByID(questSaveID);
            if(questDataSO == null)
            {
                Debug.LogWarning($"Quest with ID {questSaveID} not found in the database.");
                continue;
            }

            QuestData questToLoad = new QuestData(questDataSO);
            questToLoad.currentAmount = progress;

            activeQuests.Add(questToLoad);
        }
    }

    public void SaveData(ref GameData data)
    {
        data.activeQuests.Clear();

        foreach(var quest in activeQuests)
        {
            data.activeQuests.Add(quest.questDataSO.questSaveID, quest.currentAmount);
        }

        foreach(var quest in completedQuests)
        {
            data.completedQuests.Add(quest.questDataSO.questSaveID, true);
        }
    }
}