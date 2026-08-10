using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UI_ItemSlot : MonoBehaviour, IPointerDownHandler
{
    public Inventory_Item itemInSlot { get; private set; }
    private Inventory_Player inventory;

    [Header("UI Slot Setup")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemStackSize;

    private void Awake()
    {
        inventory = FindAnyObjectByType<Inventory_Player>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(itemInSlot == null) return;

        inventory.TryEquipItem(itemInSlot);
    }

    public void UpdateSlot(Inventory_Item item)
    {
        itemInSlot = item;

        if (itemInSlot == null)
        {
            itemStackSize.text = string.Empty;
            itemIcon.color = Color.clear;
            return;
        }
        Color color = Color.white;
        color.a = .9f;
        itemIcon.color = color;
        itemIcon.sprite = itemInSlot.itemData.itemIcon;
        itemStackSize.text = itemInSlot.stackSize > 1 ? itemInSlot.stackSize.ToString() : string.Empty;
    }
}

