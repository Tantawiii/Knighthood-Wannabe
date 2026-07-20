using System.Collections;
using UnityEngine;

public class Entity_StatusHandler : MonoBehaviour
{
    Entity entity;
    Entity_VFX entity_VFX;
    Entity_Stats entity_Stats;
    Entity_Health entity_Health;
    ElementType currentEffect = ElementType.None;

    [Header("Shock Effect Details")]
    [SerializeField] GameObject lightningStrikeVFX;
    [SerializeField] float currentCharge;
    [SerializeField] float maxCharge = 1;
    Coroutine shockCo;

    private void Awake()
    { 
        entity = GetComponent<Entity>();
        entity_VFX = GetComponent<Entity_VFX>();
        entity_Stats = GetComponent<Entity_Stats>();
        entity_Health = GetComponent<Entity_Health>();
    }

    public void ApplyStatusEffect(ElementType element, ElementalEffectData effectData)
    {
        if (element == ElementType.Ice && CanBeApplied(ElementType.Ice))
            ApplyChillEffect(effectData.chillDuration, effectData.chillSlowMultiplier);

        if (element == ElementType.Fire && CanBeApplied(ElementType.Fire))
            ApplyBurnEffect(effectData.burnDuration, effectData.burnDamage);

        if (element == ElementType.Lightning && CanBeApplied(ElementType.Lightning))
            ApplyShockEffect(effectData.shockDuration, effectData.shockDamage, effectData.shockCharge);
    }

    public void ApplyShockEffect(float duration, float electrifyDamage, float charge)
    {
        float lightningResistance = entity_Stats.GetElementalResistance(ElementType.Lightning);
        float finalcharge = charge * (1 - lightningResistance);
        currentCharge += finalcharge;

        if(currentCharge >= maxCharge)
        {
            LightningStrike(electrifyDamage);
            StopShockEffect();
            return;
        }

        if(shockCo != null)
            StopCoroutine(shockCo);

        shockCo = StartCoroutine(ShockEffectCo(duration));
    }

    public void StopShockEffect()
    {
        currentEffect = ElementType.None;
        currentCharge = 0;
        entity_VFX.StopAllVFX();
    }

    private void LightningStrike(float electrifyDamage)
    {
        Instantiate(lightningStrikeVFX, transform.position, Quaternion.identity);
        entity_Health.ReduceHealth(electrifyDamage);
    }

    IEnumerator ShockEffectCo(float duration)
    {
        currentEffect = ElementType.Lightning;
        entity_VFX.PlayOnStatusVfx(duration, ElementType.Lightning);

        yield return new WaitForSeconds(duration);
        StopShockEffect();
    }

    public void ApplyBurnEffect(float duration, float fireDamage)
    {
        float fireResistance = entity_Stats.GetElementalResistance(ElementType.Fire);
        float reducedDamage = fireDamage * (1 - fireResistance);

        StartCoroutine(BurnEffectCo(duration, reducedDamage));
    }

    IEnumerator BurnEffectCo(float duration, float totalDamage)
    {
        currentEffect = ElementType.Fire;
        entity_VFX.PlayOnStatusVfx(duration, ElementType.Fire);

        int ticksPerSecond = 2;
        int tickCount = Mathf.RoundToInt(ticksPerSecond * duration);

        float damagePerTick = totalDamage / tickCount;
        float tickInterval = 1f / ticksPerSecond;

        for (int i = 0; i < tickCount; i++)
        {
            entity_Health.ReduceHealth(damagePerTick);
            yield return new WaitForSeconds(tickInterval);
        }

        currentEffect = ElementType.None;
    }

    public void ApplyChillEffect(float duration, float slowMultiplier)
    {
        float iceResistance = entity_Stats.GetElementalResistance(ElementType.Ice);
        float reducedDuration = duration * (1 - iceResistance);

        StartCoroutine(ChillEffectCo(reducedDuration, slowMultiplier));
    }

    IEnumerator ChillEffectCo(float duration, float slowMultiplier)
    {
        entity.SlowDownEntity(duration, slowMultiplier);
        currentEffect = ElementType.Ice;
        entity_VFX.PlayOnStatusVfx(duration, ElementType.Ice);

        yield return new WaitForSeconds(duration);

        currentEffect = ElementType.None;
    }

    public bool CanBeApplied(ElementType element)
    {
        if(element == ElementType.Lightning && currentEffect == ElementType.Lightning)
            return true;

        return currentEffect == ElementType.None;
    }
}
