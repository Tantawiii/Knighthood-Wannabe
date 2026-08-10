using System.Collections.Generic;
using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    private Inventory_Player inventory;
    private UI_ItemSlot[] uiItemSlots;
    private UI_EquipmentSlot[] uiEquipmentSlots;

    [SerializeField] private Transform uiItemSlotParent;
    [SerializeField] private Transform uiEquipmentSlotParent;

    private void Awake()
    {
        uiItemSlots = uiItemSlotParent.GetComponentsInChildren<UI_ItemSlot>();
        uiEquipmentSlots = uiEquipmentSlotParent.GetComponentsInChildren<UI_EquipmentSlot>();

        inventory = FindFirstObjectByType<Inventory_Player>();
        inventory.OnInventoryChanged += UpdateUI;

        UpdateUI();
    }

    private void UpdateUI()
    {
        UpdateInventorySlots();
        UpdateEquipmentSlots();
    }

    private void UpdateEquipmentSlots()
    {
        List<Inventory_EquipmentSlot> playerEquipmentList = inventory.equipmentList;

        for(int i = 0; i < uiEquipmentSlots.Length; i++)
        {
            var playerEquipSlot = playerEquipmentList[i];

            if (!playerEquipSlot.HasItem())
            {
                uiEquipmentSlots[i].UpdateSlot(null);
            }
            else
            {
                uiEquipmentSlots[i].UpdateSlot(playerEquipSlot.equippedItem);
            }
        }
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
