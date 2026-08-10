using System;

[Serializable]
public class Inventory_Item
{
    public Item_DataSO itemData;
    public int stackSize = 1;

    public ItemModifier[] modifiers {get; private set;}

    public Inventory_Item(Item_DataSO itemData)
    {
        this.itemData = itemData;

        modifiers = GetEquipmentData()?.modifiers;
    }

    public void AddModifiers(Entity_Stats playerStats)
    {
        if (modifiers == null) return;

        foreach (var modifier in modifiers)
        {
            Stat stat = playerStats.GetStatByType(modifier.statType);
            stat.AddModifier(modifier.value, itemData.itemName);
        }
    }

    public void RemoveModifiers(Entity_Stats playerStats)
    {
        if (modifiers == null) return;

        foreach (var modifier in modifiers)
        {
            Stat stat = playerStats.GetStatByType(modifier.statType);
            stat.RemoveModifier(itemData.itemName);
        }
    }

    private Equipment_DataSO GetEquipmentData()
    {
        if (itemData is Equipment_DataSO equipmentData)
        {
            return equipmentData;
        }
        return null;
    }

    public bool CanAddStack() => stackSize < itemData.maxStackSize;
    
    public void AddToStack() => stackSize++;

    public void RemoveFromStack() => stackSize--;
}
