using System.Collections.Generic;
using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    private Inventory_Player inventory;
    private UI_ItemSlot[] uiItemSlots;
    private UI_EquipmentSlot[] uiEquipmentSlots;

    [SerializeField] private UI_ItemSlotParent inventorySlotParent;
    [SerializeField] private Transform uiEquipmentSlotParent;

    private void Awake()
    {
        uiEquipmentSlots = uiEquipmentSlotParent.GetComponentsInChildren<UI_EquipmentSlot>();

        inventory = FindFirstObjectByType<Inventory_Player>();
        inventory.OnInventoryChanged += UpdateUI;

        UpdateUI();
    }

    private void UpdateUI()
    {
        inventorySlotParent.UpdateSlots(inventory.inventoryItems);
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
}
