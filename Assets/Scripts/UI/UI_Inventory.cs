using System.Collections.Generic;
using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    private UI_ItemSlot[] uiItemSlots;
    private Inventory_Base inventory;

    private void Awake()
    {
        uiItemSlots = GetComponentsInChildren<UI_ItemSlot>();

        inventory = FindFirstObjectByType<Inventory_Base>();
        inventory.OnInventoryChanged += UpdateInventorySlots;

        UpdateInventorySlots();
    }

    private void UpdateInventorySlots()
    {
        List<Inventory_Item> inventoryItems = inventory.inventoryItems;

        for(int i = 0; i < uiItemSlots.Length; i++)
        {
            if (i < inventoryItems.Count)
            {
                uiItemSlots[i].UpdateSlot(inventoryItems[i]);
            }
            else
            {
                uiItemSlots[i].UpdateSlot(null);
            }
        }
    }
}
