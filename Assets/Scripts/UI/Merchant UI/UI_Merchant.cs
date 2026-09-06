using UnityEngine;

public class UI_Merchant : MonoBehaviour
{
    private Inventory_Merchant merchant;
    private Inventory_Player inventory;

    [SerializeField] private UI_ItemSlotParent merchantSlots;
    [SerializeField] private UI_ItemSlotParent inventorySlots;
    [SerializeField] private UI_EquipSlotParent equipSlots;

    public void SetUpMerchantUI(Inventory_Merchant merchant, Inventory_Player inventory)
    {
        this.merchant = merchant;
        this.inventory = inventory;

        this.inventory.OnInventoryChanged += UpdateSlotUI;
        this.merchant.OnInventoryChanged += UpdateSlotUI;
        
        UpdateSlotUI();

        SetUpSlots(merchantSlots);
        SetUpSlots(inventorySlots);
    }

    private void SetUpSlots(UI_ItemSlotParent slotParent)
    {
        UI_MerchantSlot[] slots = slotParent.GetComponentsInChildren<UI_MerchantSlot>();

        foreach (var slot in slots)
        {
            slot.SetUpMerchantSlot(merchant);
        }
    }

    private void UpdateSlotUI()
    {
        if(inventory == null)
        {
            return;
        }

        merchantSlots.UpdateSlots(merchant.itemList);
        inventorySlots.UpdateSlots(inventory.itemList);
        equipSlots.UpdateEquipSlots(inventory.equipmentList);
    }
}
