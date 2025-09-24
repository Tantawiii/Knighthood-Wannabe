using System;
using UnityEngine;

[Serializable]
public class Stat
{
    [SerializeField] float baseValue;

    public float GetValue()
    {
        return baseValue;
    }
}
