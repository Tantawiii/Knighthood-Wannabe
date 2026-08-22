using UnityEngine;

public class UI_Craft : MonoBehaviour
{
    [SerializeField] private UI_ItemSlotParent inventoryParent;
    private Inventory_Player inventory;
    private UI_CraftPreview craftPreviewUI;
    private UI_CraftSlot[] craftSlots;
    private UI_CraftListBTN[] craftListBTNs;

    public void SetUpCraftUI(Inventory_Storage storage)
    {
        inventory = storage.playerInventory;

        inventory.OnInventoryChanged += UpdateUI;

        craftPreviewUI = GetComponentInChildren<UI_CraftPreview>();

        craftPreviewUI.SetUpCraftPreview(storage);

        SetUpCraftListBTNs();
    }

    private void SetUpCraftListBTNs()
    {
        craftSlots = GetComponentsInChildren<UI_CraftSlot>();
        craftListBTNs = GetComponentsInChildren<UI_CraftListBTN>();

        foreach(var slot in craftSlots)
        {
            slot.gameObject.SetActive(false);
        }

        foreach(var btn in craftListBTNs)
        {
            btn.SetCraftSlots(craftSlots);
        }
    }

    private void UpdateUI() => inventoryParent.UpdateSlots(inventory.inventoryItems);
}
