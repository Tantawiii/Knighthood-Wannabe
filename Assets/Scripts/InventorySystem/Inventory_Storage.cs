using System.Collections.Generic;
using UnityEngine;

public class Inventory_Storage : Inventory_Base
{
    public Inventory_Player playerInventory { get; private set; }
    public List<Inventory_Item> materialStorage;

    public void ConsumeMaterials(Inventory_Item itemToCraft)
    {
        foreach(var requiredMaterial in itemToCraft.itemData.craftRecipe)
        {
            int amountToConsume = requiredMaterial.stackSize;

            amountToConsume -= ConsumedMaterialsAmount(playerInventory.itemList, requiredMaterial);

            if (amountToConsume > 0)
            {
                amountToConsume -= ConsumedMaterialsAmount(itemList, requiredMaterial);
            }

            if (amountToConsume > 0)
            {
                amountToConsume -= ConsumedMaterialsAmount(materialStorage, requiredMaterial);
            }
        }

        TriggerUpdateUI();
    }

    private int ConsumedMaterialsAmount(List<Inventory_Item> itemList, Inventory_Item neededItem)
    {
        int amountNeeded = neededItem.stackSize;
        int consumedAmount = 0;

        foreach (var item in itemList)
        {
            if (item.itemData != neededItem.itemData)
            {
                continue;
            }

            int removeAmount = Mathf.Min(item.stackSize, amountNeeded - consumedAmount);
            item.stackSize -= removeAmount;
            consumedAmount += removeAmount;

            if(item.stackSize <= 0)
            {
                itemList.Remove(item);
            }

            if(consumedAmount >= amountNeeded)
            {
                break;
            }
        }

        return consumedAmount;
    }

    public bool HasEnoughMaterials(Inventory_Item itemToCraft)
    {
        foreach(var requiredMaterial in itemToCraft.itemData.craftRecipe)
        {
            if(GetAvailableAmountOf(requiredMaterial.itemData) < requiredMaterial.stackSize)
            {
                return false;
            }
        }
        return true;
    }

    public int GetAvailableAmountOf(Item_DataSO requiredItem)
    {
        int amount = 0;

        foreach(var item in playerInventory.itemList)
        {
            if(item.itemData == requiredItem)
            {
                amount += item.stackSize;
            }
        }

        foreach(var item in itemList)
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
