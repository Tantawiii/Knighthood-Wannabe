using System.Collections;
using UnityEngine;

public class Entity_VFX : MonoBehaviour
{
    SpriteRenderer spriteRenderer;

    [Header("On Damage VFX")]
    [SerializeField] Material onDamageMaterial;
    [SerializeField] float onDamageVFXDuration = .15f;
    Material originalMaterial;
    Coroutine onDamageVFXCoroutine;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        originalMaterial = spriteRenderer.material;
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
