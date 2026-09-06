using UnityEngine;

public class UI_PlayerStats : MonoBehaviour
{
    private UI_StatSlot[] uiStatSlots;
    private Inventory_Player inventory;

    private void Awake()
    {
        uiStatSlots = GetComponentsInChildren<UI_StatSlot>();

        inventory = FindFirstObjectByType<Inventory_Player>();
        inventory.OnInventoryChanged += UpdateStatSlots;
    }

    private void Start()
    {
        UpdateStatSlots();
    }

    private void UpdateStatSlots()
    {
        foreach (var statSlot in uiStatSlots)
        {
            statSlot.UpdateStatValue();
        }
    }
}
