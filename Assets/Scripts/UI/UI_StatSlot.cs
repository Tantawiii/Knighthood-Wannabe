using TMPro;
using UnityEngine;

public class UI_StatSlot : MonoBehaviour
{
    private Entity_Stats playerStats;
    private RectTransform rect;
    private UI ui;

    [SerializeField] private StatType statSlotType;
    [SerializeField] private TextMeshProUGUI statName;
    [SerializeField] private TextMeshProUGUI statValue;

    private void OnValidate()
    {
        gameObject.name = "UI_Stat - " + GetStatType(statSlotType);
        statName.text = GetStatType(statSlotType);
    }
    
    private void Awake()
    {
        ui = GetComponentInParent<UI>();
        rect = GetComponent<RectTransform>();
        playerStats = FindFirstObjectByType<Entity_Stats>();
    }

    public void UpdateStatValue()
    {
        Stat statToUpdate = playerStats.GetStatByType(statSlotType);
        
        if(statToUpdate == null && statSlotType != StatType.ElementalDamage)
        {
            Debug.LogWarning($"Stat of type {statSlotType} not found in player stats.");
            return;
        }

        float value = 0;
        
        switch(statSlotType)
        {
            //Major Stats
            case StatType.Strength:
                value = playerStats.majorGroup.strength.GetValue();
                break;
            case StatType.Agility:
                value = playerStats.majorGroup.agility.GetValue();
                break;
            case StatType.Intelligence:
                value = playerStats.majorGroup.intelligence.GetValue();
                break;
            case StatType.Vitality:
                value = playerStats.majorGroup.vitality.GetValue();
                break;
            //Offense Stats
            case StatType.Damage:
                value = playerStats.GetBaseDamage();
                break;
            case StatType.CritChance:
                value = playerStats.GetCritChance();
                break;
            case StatType.CritPower:
                value = playerStats.GetCritPower();
                break;
            case StatType.AttackSpeed:
                value = playerStats.offenseGroup.attackSpeed.GetValue() * 100;
                break;
            case StatType.ArmorReduction:
                value = playerStats.GetArmorReduction() * 100;
                break;
            //Resource Stats
            case StatType.MaxHealth:
                value = playerStats.GetMaxHealth();
                break;
            case StatType.HealthRegen:
                value = playerStats.resourceGroup.healthRegen.GetValue();
                break;
            //Defense Stats
            case StatType.Evasion:
                value = playerStats.GetEvasion();
                break;
            case StatType.Armor:
                value = playerStats.GetBaseArmor();
                break;
            //Elemental Damage Stats
            case StatType.FireDamage:
                value = playerStats.offenseGroup.fireDamage.GetValue();
                break;
            case StatType.IceDamage:
                value = playerStats.offenseGroup.iceDamage.GetValue();
                break;
            case StatType.LightningDamage:
                value = playerStats.offenseGroup.lightningDamage.GetValue();
                break;
            case StatType.ElementalDamage:
                value = playerStats.GetElementalDamage(out ElementType element, 1);
                break;
            //Elemental Resistance Stats
            case StatType.FireResistance:
                value = playerStats.GetElementalResistance(ElementType.Fire) * 100;
                break;
            case StatType.IceResistance:
                value = playerStats.GetElementalResistance(ElementType.Ice) * 100;
                break;
            case StatType.LightningResistance:
                value = playerStats.GetElementalResistance(ElementType.Lightning) * 100;
                break;
        }

        statValue.text = IsPercentageStat(statSlotType) ? value.ToString("0.##") + "%" : value.ToString();
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
            case StatType.ElementalDamage: return "Elemental Damage";
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
