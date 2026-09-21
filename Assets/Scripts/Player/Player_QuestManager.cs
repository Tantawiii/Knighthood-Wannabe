using System.Collections.Generic;
using UnityEngine;

public class Player_QuestManager : MonoBehaviour
{
    public List<QuestData> activeQuests;

    public void AddProgress(string questTargetId, int amount = 1)
    {
        foreach(var quest in activeQuests)
        {
            if(quest.questDataSO.questTargetID != questTargetId)
            {
                continue;
            }

            quest.AddQuestProgress(amount);
        }
    }

    public void AcceptQuest(Quest_DataSO questDataSO)
    {
        activeQuests.Add(new QuestData(questDataSO));
    }

    public bool QuestIsActive(Quest_DataSO questToCheck)
    {
        if(questToCheck == null)
        {
            return false;
        }

        return activeQuests.Find(q => q.questDataSO == questToCheck) != null;
    }
}