using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Inventory_Storage : Inventory_Base
{
    public Inventory_Player playerInventory { get; private set; }
    public List<Inventory_Item> materialStorage;

    public void CraftItem(Inventory_Item itemToCraft)
    {
        ConsumeMaterials(itemToCraft);
        playerInventory.AddItem(itemToCraft);
    }

    public bool CanCraftItem(Inventory_Item itemToCraft)
    {
        return HasEnoughMaterials(itemToCraft) && playerInventory.CanAddItem(itemToCraft);
    }

    private void ConsumeMaterials(Inventory_Item itemToCraft)
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

    private bool HasEnoughMaterials(Inventory_Item itemToCraft)
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
            var newItemToAdd = new Inventory_Item(itemToAdd.itemData);
            materialStorage.Add(newItemToAdd);
        }
        
        TriggerUpdateUI();
        materialStorage = materialStorage.OrderBy(item => item.itemData.itemName).ToList();
    }

    public Inventory_Item StackableInStash(Inventory_Item itemToAdd)
    {
        return materialStorage.Find(item => item.itemData == itemToAdd.itemData && item.CanAddStack());
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

    public override void SaveData(ref GameData data)
    {
        base.SaveData(ref data);

        data.storageItems.Clear();

        foreach(var item in itemList)
        {
            if(item != null && item.itemData != null)
            {
                string saveID = item.itemData.saveID;

                if(!data.storageItems.ContainsKey(saveID))
                {
                    data.storageItems[saveID] = 0;
                }

                data.storageItems[saveID] += item.stackSize;
            }
        }

        data.storageMaterials.Clear();

        foreach(var item in materialStorage)
        {
            if(item != null && item.itemData != null)
            {
                string saveID = item.itemData.saveID;

                if(!data.storageMaterials.ContainsKey(saveID))
                {
                    data.storageMaterials[saveID] = 0;
                }

                data.storageMaterials[saveID] += item.stackSize;
            }
        }
    }

    public override void LoadData(GameData data)
    {
        itemList.Clear();
        materialStorage.Clear();

        foreach(var entry in data.storageItems)
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

        foreach(var entry in data.storageMaterials)
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
                AddMaterialToStorage(itemToLoad);
            }
        }
    }
}
