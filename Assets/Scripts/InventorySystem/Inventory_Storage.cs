using System.Collections.Generic;
using UnityEngine;

public class Inventory_Storage : Inventory_Base
{
    public Inventory_Player playerInventory { get; private set; }
    public List<Inventory_Item> materialStorage;

    public int GetAvailableAmountOf(Item_DataSO requiredItem)
    {
        int amount = 0;

        foreach(var item in playerInventory.inventoryItems)
        {
            if(item.itemData == requiredItem)
            {
                amount += item.stackSize;
            }
        }

        foreach(var item in inventoryItems)
        {
            if(item.itemData == requiredItem)
            {
                amount += item.stackSize;
            }
        }

        foreach(var item in materialStorage)
        {
            if(item.itemData == requiredItem)
            {
                amount += item.stackSize;
            }
        }

        return amount;
    }

    public void AddMaterialToStorage(Inventory_Item itemToAdd)
    {
        var stackableItem = StackableInStash(itemToAdd);

        if (stackableItem != null)
        {
            stackableItem.AddToStack();
        }
        else
        {
            materialStorage.Add(itemToAdd);
        }
        
        TriggerUpdateUI();
    }

    public Inventory_Item StackableInStash(Inventory_Item itemToAdd)
    {
        List<Inventory_Item> stackableItems = materialStorage.FindAll(item => item.itemData == itemToAdd.itemData);
        
        foreach (var stackableItem in stackableItems)
        {
            if (stackableItem.CanAddStack())
            {
                return stackableItem;
            }
        }

        return null;
    }

    public void SetInventory(Inventory_Player inventory) => this.playerInventory = inventory;

    public void FromPlayerToStorage(Inventory_Item item, bool transferFullStack)
    {
        int transferFullAmount = transferFullStack ? item.stackSize : 1;

        for (int i = 0; i < transferFullAmount; i++)
        {
            if(CanAddItem(item))
            {
                var itemToAdd = new Inventory_Item(item.itemData);

                playerInventory.RemoveOneItem(item);
                AddItem(itemToAdd);
            }
        }

        TriggerUpdateUI();
    }

    public void FromStorageToPlayer(Inventory_Item item, bool transferFullStack)
    {
        int transferFullAmount = transferFullStack ? item.stackSize : 1;

        for (int i = 0; i < transferFullAmount; i++)
        {
            if(playerInventory.CanAddItem(item))
            {
                var itemToAdd = new Inventory_Item(item.itemData);

                RemoveOneItem(item);
                playerInventory.AddItem(itemToAdd);
            }
        }

        TriggerUpdateUI();
    }
}
