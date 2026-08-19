using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Base : MonoBehaviour
{
    public event Action OnInventoryChanged; 
    
    public int maxInventorySize = 10;
    public List<Inventory_Item> inventoryItems = new List<Inventory_Item>();

    protected virtual void Awake()
    {

    }

    public void TryUseItem(Inventory_Item itemToUse)
    {
        Inventory_Item consumable = inventoryItems.Find(item => item == itemToUse); 

        if(consumable == null)
        {
            return;
        }

        consumable.itemEffect.ExecuteEffect();

        if(consumable.stackSize > 1)
        {
            consumable.RemoveFromStack();
        }
        else
        {
            RemoveOneItem(consumable);
        }

        OnInventoryChanged?.Invoke();
    }

    public bool CanAddItem(Inventory_Item itemToAdd)
    {
        bool hasStackable = FindStackableItem(itemToAdd) != null;
        return inventoryItems.Count < maxInventorySize || hasStackable; 
    }

    public Inventory_Item FindStackableItem(Inventory_Item itemToAdd)
    {
        List<Inventory_Item> stackableItems = inventoryItems.FindAll(item => item.itemData == itemToAdd.itemData);

        foreach (var stackableItem in stackableItems)
        {
            if (stackableItem.CanAddStack())
            {
                return stackableItem;
            }
        }

        return null;
    }


    public virtual void AddItem(Inventory_Item itemToAdd)
    {
        var existingStackable = FindStackableItem(itemToAdd);

        if(existingStackable != null)
        {
            existingStackable.AddToStack();
        }
        else
        {
            inventoryItems.Add(itemToAdd);
        }
        
        OnInventoryChanged?.Invoke();
    }

    public void RemoveOneItem(Inventory_Item itemToRemove)
    {
        Inventory_Item itemInInventory = inventoryItems.Find(item => item == itemToRemove);

        if(itemInInventory.stackSize > 1)
        {
            itemInInventory.RemoveFromStack();
        }
        else
        {
            inventoryItems.Remove(itemInInventory);
        }

        OnInventoryChanged?.Invoke();
    }

    public Inventory_Item FindItem(Item_DataSO itemData)
    {
        return inventoryItems.Find(item => item.itemData == itemData);
    }

    public void TriggerUpdateUI() => OnInventoryChanged?.Invoke();
}
