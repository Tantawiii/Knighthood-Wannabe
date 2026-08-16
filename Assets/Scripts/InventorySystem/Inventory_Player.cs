using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    private Player player;
    public List<Inventory_EquipmentSlot> equipmentList;
    
    protected override void Awake()
    {
        base.Awake();

        player = GetComponent<Player>();
    }

    public void TryEquipItem(Inventory_Item item)
    {
        var inventoryItem = FindItem(item.itemData);

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

        EquipItemToSlot(inventoryItem, slotToReplace);
        UnequipItemFromSlot(itemToUnequip);
    }

    private void EquipItemToSlot(Inventory_Item item, Inventory_EquipmentSlot slot)
    {
        float savedHealthPercent = player.health.GetCurrentHealth(); // Save current health percentage before equipping

        slot.equippedItem = item;
        item.AddModifiers(player.entityStats);

        player.health.SetCurrentHealth(savedHealthPercent); // Restore health percentage after equipping

        RemoveItem(item);
    }

    public void UnequipItemFromSlot(Inventory_Item unEquipItem)
    {
        if (!CanAddItem())
        {
            return;
        }

        float savedHealthPercent = player.health.GetCurrentHealth(); // Save current health percentage before unequipping

        var slotUnequip = equipmentList.Find(slot => slot.equippedItem == unEquipItem);

        if(slotUnequip != null)
        {
            slotUnequip.equippedItem = null;
        }

        unEquipItem.RemoveModifiers(player.entityStats);
        player.health.SetCurrentHealth(savedHealthPercent); // Restore health percentage after unequipping
        AddItem(unEquipItem);
    }
}
