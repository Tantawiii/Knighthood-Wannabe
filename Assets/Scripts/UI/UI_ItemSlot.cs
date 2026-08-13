using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UI_ItemSlot : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Inventory_Item itemInSlot { get; private set; }
    protected Inventory_Player inventory;
    protected UI ui;
    protected RectTransform rect;

    [Header("UI Slot Setup")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemStackSize;

    protected void Awake()
    {
        ui = GetComponentInParent<UI>();
        rect = GetComponent<RectTransform>();
        inventory = FindAnyObjectByType<Inventory_Player>();
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if(itemInSlot == null || itemInSlot.itemData.itemType == ItemType.Material) return;

        inventory.TryEquipItem(itemInSlot);

        if(itemInSlot == null)
        {
            ui.itemToolTip.ShowToolTip(false, null);
        }
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(itemInSlot == null) return;

        ui.itemToolTip.ShowToolTip(true, rect, itemInSlot);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        ui.itemToolTip.ShowToolTip(false, null);
    }

}

