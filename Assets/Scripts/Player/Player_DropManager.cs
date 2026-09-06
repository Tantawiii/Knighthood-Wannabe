using System.Collections.Generic;
using UnityEngine;

public class Player_DropManager : Entity_DropManager
{
    [Header("Player Drop Details")]
    [Range(0, 100)]
    [SerializeField] private float chanceToLoseItem = 90f; // Chance to lose an item on death (0-100);
    private Inventory_Player inventory;

    private void Awake()
    {
        inventory = GetComponent<Inventory_Player>();
    }

    public override void DropItems()
    {
        List<Inventory_Item> inventoryCopy = new List<Inventory_Item>(inventory.itemList); // Create a copy of the inventory list to avoid modifying it while iterating
        List<Inventory_EquipmentSlot> equipmentCopy = new List<Inventory_EquipmentSlot>(inventory.equipmentList); // Create a copy of the equipment list to avoid modifying it while iterating


        foreach(var item in inventoryCopy)
        {
            if (Random.Range(0f, 100f) < chanceToLoseItem)
            {
                CreateItemDrop(item.itemData);
                inventory.RemoveAllItems(item);
            }
        }

        foreach(var equipment in equipmentCopy)
        {
            if (equipment.HasItem() && Random.Range(0f, 100f) < chanceToLoseItem)
            {
                var item = equipment.equippedItem;

                CreateItemDrop(item.itemData);
                inventory.UnequipItemFromSlot(item);
                inventory.RemoveAllItems(item);
            }
        }
    }
}
