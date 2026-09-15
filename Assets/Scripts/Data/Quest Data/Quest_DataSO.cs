using UnityEditor;
using UnityEngine;

public enum RewardGiver
{
    Merchant,
    Blacksmith,
    None
}

[CreateAssetMenu(menuName = "RPG Setup/Quest Data/New Quest", fileName = "Quest - ")]
public class Quest_DataSO : ScriptableObject
{
    public string questSaveID;
    [Space]
    public string questName;
    [TextArea] public string questDescription;
    [TextArea] public string questObjective;

    public string questTargetID; // Enemy name, NPC name, Item name, etc.
    public int requiredAmount; // How many of the target is required to complete the quest

    [Header("Quest Rewards")]
    public RewardGiver rewardGiver;
    public Inventory_Item[] questRewards;
    public int goldReward;

    private void OnValidate()
    {
        #if UNITY_EDITOR
            string path = AssetDatabase.GetAssetPath(this);
            questSaveID = AssetDatabase.AssetPathToGUID(path);
        #endif
    }
}
