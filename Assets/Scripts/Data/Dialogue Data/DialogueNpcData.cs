using System;

[Serializable]
public class DialogueNpcData
{
    public RewardGiver rewardGiver;
    public Quest_DataSO[] quests;

    public DialogueNpcData(RewardGiver rewardGiver, Quest_DataSO[] quests)
    {
        this.rewardGiver = rewardGiver;
        this.quests = quests;
    }
}
