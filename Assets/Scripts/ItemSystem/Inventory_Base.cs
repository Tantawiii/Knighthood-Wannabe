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

    public bool CanAddItem() => inventoryItems.Count < maxInventorySize;

    public Inventory_Item StackableItem(Inventory_Item itemToAdd)
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
        var existingStackable = StackableItem(itemToAdd);

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

    public void RemoveItem(Inventory_Item itemToRemove)
    {
        inventoryItems.Remove(FindItem(itemToRemove.itemData));

        OnInventoryChanged?.Invoke();
    }

    public Inventory_Item FindItem(Item_DataSO itemData)
    {
        return inventoryItems.Find(item => item.itemData == itemData);
    }
}
