using System.Collections.Generic;
using UnityEngine;

public class UI_Inventory : MonoBehaviour
{
    private Inventory_Player inventory;

    [SerializeField] private UI_ItemSlotParent inventorySlotParent;
    [SerializeField] private UI_EquipSlotParent equipSlotParent;

    private void Awake()
    {
        inventory = FindFirstObjectByType<Inventory_Player>();
        inventory.OnInventoryChanged += UpdateUI;

        UpdateUI();
    }

    private void UpdateUI()
    {
        inventorySlotParent.UpdateSlots(inventory.itemList);
        equipSlotParent.UpdateEquipSlots(inventory.equipmentList);
    }
}
