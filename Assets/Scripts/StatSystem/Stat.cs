using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Stat
{
    [SerializeField] float baseValue;
    [SerializeField] List<StatModifier> modifiers = new List<StatModifier>();

    bool needToRecalculate = true;
    float finalValue;

    public float GetValue()
    {
        if (needToRecalculate)
        {
            finalValue = GetFinalValue();
            needToRecalculate = false;
        }

        return finalValue;
    }

    public void AddModifier(float value, string source)
    {
        StatModifier modifierToAdd = new StatModifier(value, source);
        modifiers.Add(modifierToAdd);
        needToRecalculate = true;
    }

    public void RemoveModifier(string source)
    {
        modifiers.RemoveAll(modifier => modifier.source == source);
        needToRecalculate = true;
    }

    float GetFinalValue()
    {
        float finalValue = baseValue;

        foreach (var modifier in modifiers) 
        { 
            finalValue += modifier.value;
        }

        return finalValue;
    }

    public void SetBaseValue(float value) => baseValue = value;
}

[Serializable]
public class StatModifier
{
    public float value;
    public string source;

    public StatModifier(float value, string source)
    {
        this.value = value;
        this.source = source;
    }
}