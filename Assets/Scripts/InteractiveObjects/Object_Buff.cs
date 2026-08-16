using System;
using System.Collections;
using UnityEngine;

public class Object_Buff : MonoBehaviour
{
    Player_Stats statsToModify;

    [Header("Buff Details")]
    [SerializeField] BuffEffectData[] buffs;
    [SerializeField] string buffName;
    [SerializeField] float buffDuration = 4f;

    [Header("Floating Movement")]
    [SerializeField] float floatSpeed = 1f;
    [SerializeField] float floatRange = .1f;
    Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatRange;
        transform.position = startPosition + new Vector3(0, yOffset);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        statsToModify = collision.GetComponent<Player_Stats>();

        if (statsToModify != null && statsToModify.CanApplyBuff(buffName))
        {
            statsToModify.ApplyBuff(buffs, buffDuration, buffName);
            Destroy(gameObject);
        }
    }
}
