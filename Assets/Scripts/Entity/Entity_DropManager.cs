using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Entity_DropManager : MonoBehaviour
{
    [SerializeField] private GameObject itemDropPrefab;
    [SerializeField] private ItemList_DataSO dropData;

    [Header("Drop Restrictions")]
    [SerializeField] private int maxRarityAmount = 1200;
    [SerializeField] private int maxItemsToDrop = 3;

    // private void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.X))
    //     {
    //         DropItems();
    //     }
    // }

    public virtual void DropItems()
    {
        if(dropData == null)
        {
            Debug.LogWarning("Assign a dropData SO to the Entity_DropManager script on " + gameObject.name + " to enable item drops.");
            return;
        }


        List<Item_DataSO> itemsToDrop = RollDrops();

        int amountToDrop = Mathf.Min(itemsToDrop.Count, maxItemsToDrop);

        for (int i = 0; i < amountToDrop; i++)
        {
            CreateItemDrop(itemsToDrop[i]);
        }
    }

    protected void CreateItemDrop(Item_DataSO itemData)
    {
        GameObject newItem = Instantiate(itemDropPrefab, transform.position, Quaternion.identity);
        newItem.GetComponent<Object_ItemPickup>().SetupItem(itemData);
    }

    public List<Item_DataSO> RollDrops()
    {
        List<Item_DataSO> possibleDrops = new List<Item_DataSO>();
        List<Item_DataSO> finalDrops = new List<Item_DataSO>();
        // float maxRarityAmount = this.maxRarityAmount;

        // Step 1: Filter items based on rarity
        foreach (var item in dropData.itemList)
        {
            float dropChance = item.GetDropChance();

            if (Random.Range(0f, 100f) <= dropChance)
            {
                possibleDrops.Add(item);
            }
        }

        // Step 2: Sort by rarity and limit the number of items to drop (highest to lowest)
        possibleDrops = possibleDrops.OrderByDescending(item => item.itemRarity).ToList(); // Sort by rarity (descending)

        // Step 3: Limit the number of items to drop
        foreach (var item in possibleDrops)
        {
            if (maxRarityAmount > item.itemRarity)
            {
                finalDrops.Add(item);
                maxRarityAmount -= item.itemRarity;
            }
        }

        return finalDrops;
    }
}
