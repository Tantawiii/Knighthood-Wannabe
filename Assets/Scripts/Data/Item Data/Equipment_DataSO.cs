using System;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Equipment Item", fileName = "Equipment Data - ")]
public class Equipment_DataSO : Item_DataSO
{
    [Header("Item Modifiers")]
    public ItemModifier[] modifiers;
}

[Serializable]
public class ItemModifier
{
    public StatType statType;
    public float value;
}
