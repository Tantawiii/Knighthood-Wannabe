using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    public event Action<int> OnQuickSlotUsed;
    public List<Inventory_EquipmentSlot> equipmentList;
    public Inventory_Storage storage {get; private set;}

    [Header("Quick Item Slots")]
    public Inventory_Item[] quickItems = new Inventory_Item[2];
    [Header("Gold Info")]
    public int gold = 0;
    
    protected override void Awake()
    {
        base.Awake();
        storage = FindFirstObjectByType<Inventory_Storage>();
    }

    public void SetQuickItemInSlot(int slotNumber, Inventory_Item itemToSet)
    {
        quickItems[slotNumber - 1] = itemToSet;
        TriggerUpdateUI();
    }

    public void TryUseQuickItem(int passedSlotNumber)
    {
        int slotNumber = passedSlotNumber - 1;
        var itemToUse = quickItems[slotNumber];

        if (itemToUse == null)
            return;

        TryUseItem(itemToUse);

        if(FindItem(itemToUse) == null)
        {
            quickItems[slotNumber] = FindSameItem(itemToUse);
        }

        TriggerUpdateUI();
        OnQuickSlotUsed?.Invoke(slotNumber);
    }

    public void TryEquipItem(Inventory_Item item)
    {
        var inventoryItem = FindItem(item);

        var matchingSlots = equipmentList.FindAll(slot => slot.slotType == item.itemData.itemType);

        // Step 1: Try to find empty slot and equip item
        foreach (var slot in matchingSlots)
        {
            if (!slot.HasItem())
            {
                EquipItemToSlot(inventoryItem, slot);
                return;
            }
        }

        // Step 2: No empty slots ? Replace first one
        var slotToReplace = matchingSlots[0];
        var itemToUnequip = slotToReplace.equippedItem;

        UnequipItemFromSlot(itemToUnequip, slotToReplace != null);
        EquipItemToSlot(inventoryItem, slotToReplace);
    }

    private void EquipItemToSlot(Inventory_Item item, Inventory_EquipmentSlot slot)
    {
        float savedHealthPercent = player.health.GetHealthPercent(); // Save current health percentage before equipping

        slot.equippedItem = item;
        item.AddModifiers(player.stats);
        slot.equippedItem.AddItemEffect(player);

        player.health.SetHealthPercent(savedHealthPercent); // Restore health percentage after equipping

        RemoveOneItem(item);
    }

    public void UnequipItemFromSlot(Inventory_Item unEquipItem, bool replacingItem = false)
    {
        if (!CanAddItem(unEquipItem) && !replacingItem)
        {
            return;
        }

        float savedHealthPercent = player.health.GetHealthPercent(); // Save current health percentage before unequipping

        var slotUnequip = equipmentList.Find(slot => slot.equippedItem == unEquipItem);

        if(slotUnequip != null)
        {
            slotUnequip.equippedItem = null;
        }

        unEquipItem.RemoveModifiers(player.stats);
        unEquipItem.RemoveItemEffect();

        player.health.SetHealthPercent(savedHealthPercent); // Restore health percentage after unequipping
        AddItem(unEquipItem);
    }

    public override void SaveData(ref GameData data)
    {
        data.gold = gold;
        data.inventory.Clear();
        data.equippedItems.Clear();

        foreach (var item in itemList)
        {
            if(item != null && item.itemData != null)
            {
                string saveID = item.itemData.saveID;

                if(!data.inventory.ContainsKey(saveID))
                {
                    data.inventory[saveID] = 0;
                }

                data.inventory[saveID] += item.stackSize;
            }
        }

        foreach (var slot in equipmentList)
        {
            if(slot.HasItem())
            {
                data.equippedItems[slot.equippedItem.itemData.saveID] = slot.slotType;
            }
        }
    }

    public override void LoadData(GameData data)
    {
        gold = data.gold;

        foreach(var entry in data.inventory)
        {
            string saveID = entry.Key;
            int stackSize = entry.Value;

            var itemData = itemDataBase.GetItemData(saveID);

            if(itemData == null)
            {
                Debug.LogWarning($"Item with saveID {saveID} not found in item database.");
                continue;
            }

            for(int i = 0; i < stackSize; i++)
            {
                Inventory_Item itemToLoad = new Inventory_Item(itemData);
                AddItem(itemToLoad);
            }
        }

        foreach(var entry in data.equippedItems)
        {
            string saveID = entry.Key;
            ItemType loadedSlotType = entry.Value;

            Item_DataSO itemData = itemDataBase.GetItemData(saveID);
            Inventory_Item itemToLoad = new Inventory_Item(itemData);

            var slot = equipmentList.Find(slot => slot.slotType == loadedSlotType && !slot.HasItem());

            slot.equippedItem = itemToLoad;
            slot.equippedItem.AddModifiers(player.stats);
            slot.equippedItem.AddItemEffect(player);
        }
        TriggerUpdateUI();
    }
}
