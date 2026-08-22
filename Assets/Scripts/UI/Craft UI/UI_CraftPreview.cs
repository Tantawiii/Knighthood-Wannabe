using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_CraftPreview : MonoBehaviour
{
    private Inventory_Item itemToCraft;
    private Inventory_Storage storage;
    private UI_CraftPreviewSlot[] craftPreviewSlots;

    [Header("Item Preview Setup")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDescription;
    [SerializeField] private TextMeshProUGUI btnText;

    public void SetUpCraftPreview(Inventory_Storage storage)
    {
        this.storage = storage;

        craftPreviewSlots = GetComponentsInChildren<UI_CraftPreviewSlot>();

        foreach(var slot in craftPreviewSlots)
        {
            slot.gameObject.SetActive(false);
        }
    }

    public void ConfirmCraft()
    {
        if(itemToCraft == null)
        {
            btnText.text = "Pick an item!";
            return;
        }

        if(storage.HasEnoughMaterials(itemToCraft) && storage.playerInventory.CanAddItem(itemToCraft))
        {
            storage.ConsumeMaterials(itemToCraft);
            storage.playerInventory.AddItem(itemToCraft);
        }

        UpdateCraftPreviewSlots();
    }

    public void UpdateCraftPreview(Item_DataSO itemData)
    {
        itemToCraft = new Inventory_Item(itemData);

        itemIcon.sprite = itemData.itemIcon;
        itemName.text = itemData.itemName;
        itemDescription.text = itemToCraft.GetItemInfo();

        UpdateCraftPreviewSlots();
    }

    private void UpdateCraftPreviewSlots()
    {
        foreach (var slot in craftPreviewSlots)
        {
            slot.gameObject.SetActive(false);
        }

        for (int i = 0; i < itemToCraft.itemData.craftRecipe.Length; i++)
        {
            Inventory_Item requiredItem = itemToCraft.itemData.craftRecipe[i];

            int availableAmount = storage.GetAvailableAmountOf(requiredItem.itemData);
            int requiredAmount = requiredItem.stackSize;

            craftPreviewSlots[i].gameObject.SetActive(true);
            craftPreviewSlots[i].SetUpPreviewSlot(requiredItem.itemData, availableAmount, requiredAmount);
        }
    }
}
