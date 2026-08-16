using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_Stats : Entity_Stats
{
    private List<string> activeBuff = new List<string>();
    private Inventory_Player inventory;

    protected override void Awake()
    {
        base.Awake();

        inventory = GetComponent<Inventory_Player>();
    }

    public bool CanApplyBuff(string buffName)
    {
        return !activeBuff.Contains(buffName);
    }

    public void ApplyBuff(BuffEffectData[] buffsToApply, float duration, string buffName)
    {
        StartCoroutine(BuffDurationCo(buffsToApply, duration, buffName));
    }

    private IEnumerator BuffDurationCo(BuffEffectData[] buffsToApply, float duration, string buffName)
    {
        activeBuff.Add(buffName);

        foreach (var buff in buffsToApply)
        {
            GetStatByType(buff.Type).AddModifier(buff.Value, buffName);
        }

        yield return new WaitForSeconds(duration);

        foreach (var buff in buffsToApply)
        {
            GetStatByType(buff.Type).RemoveModifier(buffName);
        }

        inventory.TriggerUpdateUI();
        activeBuff.Remove(buffName);
    }
}
