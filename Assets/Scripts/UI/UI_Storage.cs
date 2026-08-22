using System;
using UnityEngine;

public class UI_Storage : MonoBehaviour
{
    private Inventory_Storage storage;
    private Inventory_Player playerInventory;

    [SerializeField] private UI_ItemSlotParent inventorySlotParent;
    [SerializeField] private UI_ItemSlotParent storageSlotParent;
    [SerializeField] private UI_ItemSlotParent materialSlotParent;

    public void SetUpStorageUI(Inventory_Storage storage)
    {
        this.storage = storage;
        playerInventory = storage.playerInventory;

        storage.OnInventoryChanged += UpdateUI;
        UpdateUI();

        UI_StorageSlot[] storageSlots = GetComponentsInChildren<UI_StorageSlot>();

        foreach (var slot in storageSlots)
        {
            slot.SetStorage(storage);
        }
    }

    private void OnEnable()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if(storage == null)
        {
            return;
        }
        
        inventorySlotParent.UpdateSlots(playerInventory.inventoryItems);
        storageSlotParent.UpdateSlots(storage.inventoryItems);
        materialSlotParent.UpdateSlots(storage.materialStorage);
    }
}
