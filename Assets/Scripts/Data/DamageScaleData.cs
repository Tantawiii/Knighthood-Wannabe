using UnityEngine;
using System;

[Serializable]
public class DamageScaleData
{
    [Header("Damage Scale")]
    public float physical = 1f;
    public float elemental = 1f;

    [Header("Chill")]
    public float chillDuration = 3f;
    public float chillSlowMultiplier = .2f;

    [Header("Burn")]
    public float burnDuration = 3f;
    public float burnDamageMultiplier = 1f;

    [Header("Shock")]
    public float shockDuration = 3f;
    public float shockDamageMultiplier = 1f;
    public float shockChargeMultiplier = .4f;
}
