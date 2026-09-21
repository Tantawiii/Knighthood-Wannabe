using System;

[Serializable]
public class QuestData
{
    public Quest_DataSO questDataSO;
    public int currentAmount;
    public bool canGetReward;

    public QuestData(Quest_DataSO questDataSO)
    {
        this.questDataSO = questDataSO;
        currentAmount = 0;
        canGetReward = false;
    }

    public bool CanGetReward() => currentAmount >= questDataSO.requiredAmount;

    public void AddQuestProgress(int amount = 1)
    {
        currentAmount += amount;
        canGetReward = CanGetReward();
    }
}
