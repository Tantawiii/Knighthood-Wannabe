using System.Collections;
using UnityEngine;

public class Entity_VFX : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Entity entity;

    [Header("On Taking Damage VFX")]
    [SerializeField] Material onDamageMaterial;
    [SerializeField] float onDamageVFXDuration = .15f;
    Material originalMaterial;
    Coroutine onDamageVFXCoroutine;

    [Header("On Doing Damage VFX")]
    [SerializeField] Color hitVFXColor = Color.white;
    [SerializeField] GameObject hitVFX;
    [SerializeField] GameObject critHitVFX;

    [Header("ELement Colors")]
    [SerializeField] Color chillVFX = Color.cyan;
    [SerializeField] Color burnVFX = Color.red;
    Color originalHitVFXColor;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        entity = GetComponentInChildren<Entity>();
        originalMaterial = spriteRenderer.material;
        originalHitVFXColor = hitVFXColor;
    }

    public void PlayOnStatusVfx(float duration, ElementType element)
    {
        if (element == ElementType.Ice)
            StartCoroutine(PlayStatusVfxCo(duration, chillVFX));

        if (element == ElementType.Fire)
            StartCoroutine(PlayStatusVfxCo(duration, burnVFX));
    }

    IEnumerator PlayStatusVfxCo(float duration, Color effectColor)
    {
        float tickInterval = .25f;
        float timeHasPassed = 0;

        Color lightColor = effectColor * 1.2f;
        Color darkColor = effectColor * .95f;

        bool toggle = false;

        while (timeHasPassed < duration)
        {
            spriteRenderer.color = toggle ? lightColor : darkColor;
            toggle = !toggle;

            yield return new WaitForSeconds(tickInterval);
            timeHasPassed += tickInterval;
        }

        spriteRenderer.color = Color.white;
    }

    public void CreateOnHitVFX(Transform target, bool isCrit)
    {
        GameObject hitPrefab = isCrit ? critHitVFX : hitVFX;
        GameObject vfx = Instantiate(hitPrefab, target.position, Quaternion.identity);
        
        vfx.GetComponentInChildren<SpriteRenderer>().color = hitVFXColor;


        if(entity.facingDir == -1 && isCrit)
        {
            vfx.transform.Rotate(0, 180, 0);
        }
    }

    public void UpdateOnHitColor(ElementType element)
    {
        if (element == ElementType.Ice)
            hitVFXColor = chillVFX;
        if(element == ElementType.None)
            hitVFXColor = originalHitVFXColor;
    }

    public void PlayOnDamageVFX()
    {
        if (onDamageVFXCoroutine != null)
        {
            StopCoroutine(onDamageVFXCoroutine);
        }

        onDamageVFXCoroutine = StartCoroutine(OnDamageVFXCo());
    }

    private IEnumerator OnDamageVFXCo()
    {
        spriteRenderer.material = onDamageMaterial;
        yield return new WaitForSeconds(onDamageVFXDuration);
        spriteRenderer.material = originalMaterial;
    }
}
