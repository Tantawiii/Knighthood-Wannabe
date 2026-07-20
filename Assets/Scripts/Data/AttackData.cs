using UnityEngine;
using System;

[Serializable]
public class AttackData
{
    public float physicalDamage;
    public float elementalDamage;
    public bool isCritical;
    public ElementType elementType;
    public ElementalEffectData elementalEffectData;

    public AttackData(Entity_Stats playerStats, DamageScaleData damageScaleData)
    {
        this.physicalDamage = playerStats.GetPhysicalDamage(out isCritical, damageScaleData.physical);
        this.elementalDamage = playerStats.GetElementalDamage(out elementType, damageScaleData.elemental);
        this.elementalEffectData = new ElementalEffectData(playerStats, damageScaleData);
    }
}
