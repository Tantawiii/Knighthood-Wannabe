using UnityEngine;

[CreateAssetMenu(menuName = "RPG Setup/Item Data/Material Item", fileName = "Material Data - ")]
public class Item_DataSO : ScriptableObject
{
    [Header("Item Details")]
    public string itemName;
    public Sprite itemIcon;
    public ItemType itemType;
    public int maxStackSize = 1;

    [Header("Item Effect")]
    public ItemEffect_DataSO itemEffect;

    [Header("Craft Details")]
    public Inventory_Item[] craftRecipe;

    [Header("Merchant Details")]
    [Range(0, 10000)]
    public int itemPrice = 100;
    public int minStackSizeAtShop = 1;
    public int maxStackSizeAtShop = 1;
}
