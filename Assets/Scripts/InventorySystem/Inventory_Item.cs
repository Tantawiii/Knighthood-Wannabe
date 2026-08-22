using System;
using System.Text;

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

    public void AddItemEffect(Player player) => itemEffect?.Subscribe(player);
    public void RemoveItemEffect() => itemEffect?.Unsubscribe();

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

    public string GetItemInfo()
    {
        if(itemData.itemType == ItemType.Material)
        {
            return "Used for Crafting";
        }

        if(itemData.itemType == ItemType.Consumable)
        {
            return itemData.itemEffect.effectDescription;
        }

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("");

        foreach(var mod in modifiers)
        {
            string modType = GetStatType(mod.statType);
            string modValue = IsPercentageStat(mod.statType)
                ? mod.value.ToString("0.##") + "%"
                : mod.value.ToString();
            sb.AppendLine("+ " + modValue + " " + modType);
        }

        if(itemEffect != null)
        {
            sb.AppendLine("");
            sb.AppendLine("Unique Effect:");
            sb.AppendLine(itemEffect.effectDescription);
        }

        return sb.ToString();
    }

    private string GetStatType(StatType statType)
    {
        switch(statType)
        {
            case StatType.MaxHealth: return "Max Health";
            case StatType.HealthRegen: return "Health Regeneration";
            case StatType.Strength: return "Strength";
            case StatType.Agility: return "Agility";
            case StatType.Intelligence: return "Intelligence";
            case StatType.Vitality: return "Vitality";
            case StatType.AttackSpeed: return "Attack Speed";
            case StatType.Damage: return "Damage";
            case StatType.CritChance: return "Critical Chance";
            case StatType.CritPower: return "Critical Power";
            case StatType.ArmorReduction: return "Armor Reduction";
            case StatType.FireDamage: return "Fire Damage";
            case StatType.IceDamage: return "Ice Damage";
            case StatType.LightningDamage: return "Lightning Damage";
            case StatType.Armor: return "Armor";
            case StatType.Evasion: return "Evasion";
            case StatType.IceResistance: return "Ice Resistance";
            case StatType.FireResistance: return "Fire Resistance";
            case StatType.LightningResistance: return "Lightning Resistance";
            default: return "Unknown Stat";
        }
    }

    private bool IsPercentageStat(StatType type)
    {
        switch (type)
        {
            case StatType.CritChance:
            case StatType.CritPower:
            case StatType.ArmorReduction:
            case StatType.IceResistance:
            case StatType.FireResistance:
            case StatType.LightningResistance:
            case StatType.AttackSpeed:
            case StatType.Evasion:
                return true;

            default:
                return false;
        }
    }
}
