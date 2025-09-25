using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class Buff
{
    public StatType Type;
    public float Value;
}

public class Object_Buff : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Entity_Stats statsToModify;

    [Header("Buff Details")]
    [SerializeField] Buff[] buffs;
    [SerializeField] string buffName;
    [SerializeField] float buffDuration = 4f;
    [SerializeField] bool canBeUsed = true;

    [Header("Floating Movement")]
    [SerializeField] float floatSpeed = 1f;
    [SerializeField] float floatRange = .1f;
    Vector3 startPosition;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        startPosition = transform.position;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatRange;
        transform.position = startPosition + new Vector3(0, yOffset);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canBeUsed)
            return;

        statsToModify = collision.GetComponent<Entity_Stats>();
        StartCoroutine(BuffCo(buffDuration));
    }

    IEnumerator BuffCo(float duration)
    {
        canBeUsed = false;
        spriteRenderer.color = Color.clear;
        ApplyBuff(true);

        yield return new WaitForSeconds(duration);

        ApplyBuff(false);
        Destroy(gameObject);
    }

    private void ApplyBuff(bool apply)
    {
        foreach (var buff in buffs)
        {
            if(apply)
                statsToModify.GetStatByType(buff.Type).AddModifier(buff.Value, buffName);
            else
                statsToModify.GetStatByType(buff.Type).RemoveModifier(buffName);
        }
    }
}
