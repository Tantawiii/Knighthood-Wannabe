using UnityEngine;

public class Object_ItemPickup : MonoBehaviour
{
    private SpriteRenderer sr;
    [SerializeField] private Item_DataSO itemData;

    private void OnValidate()
    {
        if(itemData == null) return;

        sr = GetComponent<SpriteRenderer>();
        sr.sprite = itemData.itemIcon;
        gameObject.name = "Object_ItemPickup - " + itemData.itemName;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Player picked up: " + itemData.itemName);
        Destroy(gameObject);
    }
}
