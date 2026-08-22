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

        for(int i = 0; i < craftData.itemList.Length; i++)
        {
            Item_DataSO itemData = craftData.itemList[i];

            craftSlots[i].gameObject.SetActive(true);
            craftSlots[i].SetUpButton(itemData);
        }
    }
}
