using UnityEngine;

public class UI_QuickItemSlot : UI_ItemSlot
{
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private int slotNumber;

    public void SetUpQuickSlotItem(Inventory_Item itemToPass)
    {
        inventory.SetQuickItemInSlot(slotNumber, itemToPass);
    }

    public void UpdateQuickSlotUI(Inventory_Item currentItemInSlot)
    {
        if(currentItemInSlot == null || currentItemInSlot.itemData == null)
        {
            itemIcon.sprite = defaultSprite;
            itemStackSize.text = "";
            return;
        }

        itemIcon.sprite = currentItemInSlot.itemData.itemIcon;
        itemStackSize.text = currentItemInSlot.stackSize.ToString();
    }
}
