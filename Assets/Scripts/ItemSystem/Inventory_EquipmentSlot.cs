using System;

[Serializable]
public class Inventory_EquipmentSlot
{
    public ItemType slotType;
    public Inventory_Item equippedItem;

    public bool HasItem() => equippedItem != null && equippedItem.itemData != null;
}
