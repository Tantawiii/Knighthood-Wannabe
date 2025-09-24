using UnityEngine;

public class Entity_Stats : MonoBehaviour
{
    public Stat maxHealth;
    public Stat_MajorGroup majorGroup;
    public Stat_OffenseGroup offenseGroup;
    public Stat_DefenseGroup defenseGroup;

    public float GetElementalDamage(out ElementType element, float scaleFactor = 1)
    {
        float fireDamage = offenseGroup.fireDamage.GetValue();
        float iceDamage = offenseGroup.iceDamage.GetValue();
        float lightningDamage = offenseGroup.lightningDamage.GetValue();

        float bonusElementalDamage = majorGroup.intelligence.GetValue();

        float highestDamage = fireDamage;
        element = ElementType.Fire;

        if(iceDamage > highestDamage)
        {
            highestDamage = iceDamage;
            element = ElementType.Ice;
        }

        if (lightningDamage > highestDamage)
        {
            highestDamage = lightningDamage;
            element = ElementType.Lightning;
        }

        if (highestDamage <= 0)
        {
            element = ElementType.None;
            return 0;
        }   

        float bonusFire = (fireDamage == highestDamage) ? 0 : fireDamage * .5f;
        float bonusIce = (iceDamage == highestDamage) ? 0 : iceDamage * .5f;
        float bonusLightning = (lightningDamage == highestDamage) ? 0 : lightningDamage * .5f;

        float weakerElementalDamage = bonusFire + bonusIce + bonusLightning;
        float finalDamage = highestDamage + bonusElementalDamage + weakerElementalDamage;

        return finalDamage * scaleFactor;
    }

    public float GetElementalResistance(ElementType element)
    {
        float baseResistance = 0;
        float bonusResistance = majorGroup.intelligence.GetValue() * .5f;

        switch (element) {
            case ElementType.Fire:
                baseResistance = defenseGroup.fireRes.GetValue(); 
                break;
            case ElementType.Ice:
                baseResistance = defenseGroup.iceRes.GetValue();
                break;
            case ElementType.Lightning:
                baseResistance = defenseGroup.lightningRes.GetValue(); 
                break;
            default:
                break;
        }

        float resistance = baseResistance + bonusResistance;
        float resistanceCap = 75f;
        float finalResistance = Mathf.Clamp(resistance, 0, resistanceCap) / 100;

        return finalResistance;
    }

    public float GetPhysicalDamage(out bool isCrit, float scaleFactor = 1)
    {
        float baseDamage = offenseGroup.damage.GetValue();
        float bonusDamage = majorGroup.strength.GetValue();

        float totalBaseDamage = baseDamage + bonusDamage;

        float baseCritChance = offenseGroup.critChance.GetValue();
        float bonusCritChance = majorGroup.agility.GetValue() * .3f;
        
        float critChance = baseCritChance + bonusCritChance;

        float baseCritPower = offenseGroup.critPower.GetValue();
        float bonusCritPower = majorGroup.strength.GetValue() * .5f;

        float critPower = (baseCritPower + bonusCritPower) / 100;

        isCrit = Random.Range(0, 100) < critChance;

        float finalDamage = isCrit ? totalBaseDamage * critPower : totalBaseDamage;

        return finalDamage * scaleFactor;
    }

    public float GetArmorMitigation(float armorReduction)
    {
        float baseArmor = defenseGroup.armor.GetValue();
        float bonusArmor = majorGroup.vitality.GetValue();
        float totalArmor = baseArmor + bonusArmor;

        float reductionMultiplier = Mathf.Clamp01(1 - armorReduction);
        float effectiveArmor = totalArmor * reductionMultiplier;

        float mitigation = effectiveArmor / (effectiveArmor + 100);
        float mitigationCap = .85f;

        float finalMitigation = Mathf.Clamp(mitigation, 0, mitigationCap);

        return finalMitigation;
    }

    public float GetArmorReduction()
    {
        float finalReduction = offenseGroup.armorReduction.GetValue();

        return finalReduction;
    }

    public float GetEvasion()
    {
        float baseEvasion = defenseGroup.evasion.GetValue();
        float bonusEvasion = majorGroup.agility.GetValue() * .5f;

        float totalEvasion = baseEvasion + bonusEvasion;
        float evasionCap = 85f; // 85% evasion limit

        float finalEvasion = Mathf.Clamp(totalEvasion,0, evasionCap);

        return finalEvasion;
    }

    public float GetMaxHealth()
    {
        float baseMaxHealth = maxHealth.GetValue();
        float bonusMaxHealth = majorGroup.vitality.GetValue() * 5;

        float finalMaxHealth = baseMaxHealth + bonusMaxHealth;

        return finalMaxHealth;
    }
}
