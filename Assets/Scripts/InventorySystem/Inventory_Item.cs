using System;

[Serializable]
public class Inventory_Item
{
    private string itemId;

    public Item_DataSO itemData;
    public int stackSize = 1;

    public ItemModifier[] modifiers {get; private set;}
    public ItemEffect_DataSO itemEffect;

    public Inventory_Item(Item_DataSO itemData)
    {
        this.itemData = itemData;

        modifiers = GetEquipmentData()?.modifiers;

        itemEffect = itemData.itemEffect;

        itemId = itemData.itemName + " - " + Guid.NewGuid();
    }

    public void AddModifiers(Entity_Stats playerStats)
    {
        if (modifiers == null) return;

        foreach (var modifier in modifiers)
        {
            Stat stat = playerStats.GetStatByType(modifier.statType);
            stat.AddModifier(modifier.value, itemId);
        }
    }

    public void RemoveModifiers(Entity_Stats playerStats)
    {
        if (modifiers == null) return;

        foreach (var modifier in modifiers)
        {
            Stat stat = playerStats.GetStatByType(modifier.statType);
            stat.RemoveModifier(itemId);
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
