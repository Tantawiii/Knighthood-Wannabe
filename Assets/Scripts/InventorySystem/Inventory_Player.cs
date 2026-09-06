using System.Collections.Generic;
using UnityEngine;

public class Inventory_Player : Inventory_Base
{
    public int gold = 10000;
    private Player player;
    public List<Inventory_EquipmentSlot> equipmentList;
    public Inventory_Storage storage {get; private set;}
    
    protected override void Awake()
    {
        base.Awake();

        player = GetComponent<Player>();

        storage = FindFirstObjectByType<Inventory_Storage>();
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
        float savedHealthPercent = player.health.GetCurrentHealth(); // Save current health percentage before equipping

        slot.equippedItem = item;
        item.AddModifiers(player.entityStats);
        slot.equippedItem.AddItemEffect(player);

        player.health.SetCurrentHealth(savedHealthPercent); // Restore health percentage after equipping

        RemoveOneItem(item);
    }

    public void UnequipItemFromSlot(Inventory_Item unEquipItem, bool replacingItem = false)
    {
        if (!CanAddItem(unEquipItem) && !replacingItem)
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
        unEquipItem.RemoveItemEffect();

        player.health.SetCurrentHealth(savedHealthPercent); // Restore health percentage after unequipping
        AddItem(unEquipItem);
    }
}
