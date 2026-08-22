using UnityEngine;

public class UI_CraftListBTN : MonoBehaviour
{
    [SerializeField] private ItemList_DataSO craftData;
    private UI_CraftSlot[] craftSlots;

    public void SetCraftSlots(UI_CraftSlot[] craftSlots) => this.craftSlots = craftSlots;

    public void UpdateCraftSlots()
    {
        if(craftData == null)
        {
            Debug.LogWarning("Craft list is not set.");
            return;
        }


        foreach(var slot in craftSlots)
        {
            slot.gameObject.SetActive(false);
        }

        int slotCount = Mathf.Min(craftData.itemList.Length, craftSlots.Length);

        if(craftData.itemList.Length > craftSlots.Length)
            Debug.LogWarning($"{craftData.name} has {craftData.itemList.Length} items but only {craftSlots.Length} craft slots are available.");

        for(int i = 0; i < slotCount; i++)
        {
            Item_DataSO itemData = craftData.itemList[i];

            craftSlots[i].gameObject.SetActive(true);
            craftSlots[i].SetUpButton(itemData);
        }
    }
}
