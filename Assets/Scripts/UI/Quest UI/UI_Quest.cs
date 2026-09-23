using UnityEngine;

public class UI_Quest : MonoBehaviour, ISaveable
{
    private GameData currentGameData;

    [SerializeField] private UI_ItemSlotParent inventorySlots;
    [SerializeField] private UI_QuestPreview questPreview;
    private UI_QuestSlot[] questSlots;
    public Player_QuestManager questManager { get; private set; }

    private void Awake()
    {
        questSlots = GetComponentsInChildren<UI_QuestSlot>(true);
        questManager = Player.Instance.questManager;
    }

    public void SetUpQuestUI(Quest_DataSO[] questsToSetup)
    {
        foreach(var slot in questSlots)
        {
            slot.gameObject.SetActive(false);
        }

        for(int i = 0; i < questsToSetup.Length; i++)
        {
            questSlots[i].gameObject.SetActive(true);
            questSlots[i].SetupQuestSlot(questsToSetup[i]);
        }

        questPreview.MakeQuestPreviewEmpty();
        inventorySlots.UpdateSlots(Player.Instance.inventory.itemList);

        UpdateQuestList();
    }

    public void UpdateQuestList()
    {
        foreach(var slot in questSlots)
        {
            if(slot.gameObject.activeSelf && !CanTakeQuest(slot.questInSlot))
            {
                slot.gameObject.SetActive(false);
            }
        }
    }

    private bool CanTakeQuest(Quest_DataSO questToCheck)
    {
        bool questIsActive = questManager.QuestIsActive(questToCheck);

        if(currentGameData != null)
        {
            bool questIsCompleted = currentGameData.completedQuests.TryGetValue(questToCheck.questSaveID, out bool isCompleted) && isCompleted;

            return !questIsActive && !questIsCompleted;
        }

        return !questIsActive;
    }

    public UI_QuestPreview GetQuestPreview() => questPreview;

    public void LoadData(GameData data)
    {
        currentGameData = data;
    }

    public void SaveData(ref GameData data)
    {
        
    }
}
