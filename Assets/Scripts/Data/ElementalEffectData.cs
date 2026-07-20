using UnityEngine;
using System;

[Serializable]
public class ElementalEffectData
{
    public float chillDuration;
    public float chillSlowMultiplier;

    public float burnDuration;
    public float burnDamage;

    public float shockDuration;
    public float shockDamage;
    public float shockCharge;

    public ElementalEffectData(Entity_Stats stats, DamageScaleData damageScale)
    {
        chillDuration = damageScale.chillDuration;
        chillSlowMultiplier = damageScale.chillSlowMultiplier;

        burnDuration = damageScale.burnDuration;
        burnDamage = stats.offenseGroup.fireDamage.GetValue() * damageScale.burnDamageMultiplier;

        shockDuration = damageScale.shockDuration;
        shockDamage = stats.offenseGroup.lightningDamage.GetValue() * damageScale.shockDamageMultiplier;
        shockCharge = damageScale.shockChargeMultiplier;
    }
}