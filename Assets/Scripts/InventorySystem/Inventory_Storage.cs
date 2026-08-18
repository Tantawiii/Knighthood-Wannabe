using UnityEngine;

public class Inventory_Storage : Inventory_Base
{
    private Inventory_Player playerInventory;

    public void SetInventory(Inventory_Player inventory) => this.playerInventory = inventory;

    public void FromPlayerToStorage(Inventory_Item item)
    {
        if(CanAddItem(item))
        {
            playerInventory.RemoveItem(item);
            AddItem(item);
        }

        TriggerUpdateUI();
    }

    public void FromStorageToPlayer(Inventory_Item item)
    {
        if(playerInventory.CanAddItem(item))
        {
            RemoveItem(item);
            playerInventory.AddItem(item);
        }

        TriggerUpdateUI();
    }
}
